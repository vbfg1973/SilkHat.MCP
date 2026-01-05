using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Git.Analysis.Services;

public sealed class GitCli : IGitCli
{
    private readonly IGitCommandRunner _runner;
    private readonly IGitRepositoryCacheStore _cacheStore;

    public GitCli(IGitCommandRunner runner, IGitRepositoryCacheStore cacheStore)
    {
        _runner = runner;
        _cacheStore = cacheStore;
    }

    public async Task<IReadOnlyList<GitTreeEntryDto>> ListTreeAsync(
        Guid configId,
        string repoRoot,
        string? nameFilter,
        GitTreeEntryType? typeFilter,
        DateTimeOffset? changedAfter,
        string? author,
        CancellationToken cancellationToken)
    {
        var listResult = await _runner.ExecuteAsync(repoRoot, new[] { "ls-tree", "-r", "--name-only", "HEAD" }, cancellationToken);
        EnsureSuccess(listResult, "git ls-tree");

        var files = listResult.StandardOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizePathKey)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var parts = file.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                continue;
            }

            var current = string.Empty;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                current = string.IsNullOrEmpty(current) ? parts[i] : $"{current}/{parts[i]}";
                directories.Add(current);
            }
        }

        var includeDirectories = !changedAfter.HasValue && string.IsNullOrWhiteSpace(author);
        var entries = new List<GitTreeEntryDto>();
        var cache = _cacheStore.GetOrCreate(configId);

        foreach (var file in files)
        {
            if (typeFilter.HasValue && typeFilter != GitTreeEntryType.File)
            {
                continue;
            }

            if (!MatchesNameFilter(file, nameFilter))
            {
                continue;
            }

            var lastChange = await GetLastChangeAsync(cache, repoRoot, file, cancellationToken);
            if (!MatchesChangeFilters(lastChange, changedAfter, author))
            {
                continue;
            }

            entries.Add(new GitTreeEntryDto(
                NormalizePath(file),
                Path.GetFileName(file),
                GitTreeEntryType.File,
                lastChange));
        }

        if (includeDirectories)
        {
            foreach (var directory in directories.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (typeFilter.HasValue && typeFilter != GitTreeEntryType.Directory)
                {
                    continue;
                }

                if (!MatchesNameFilter(directory, nameFilter))
                {
                    continue;
                }

                entries.Add(new GitTreeEntryDto(
                    NormalizePath(directory),
                    Path.GetFileName(directory),
                    GitTreeEntryType.Directory,
                    null));
            }
        }

        return entries;
    }

    public async Task<GitFileHistoryDto> FileHistoryAsync(
        Guid configId,
        string repoRoot,
        string path,
        CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizePathKey(path);
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[] { "log", "--date=iso-strict", "--pretty=format:COMMIT|%H|%ad|%an|%s", "--name-status", "--", normalizedKey },
            cancellationToken);
        EnsureSuccess(result, "git log");

        var cache = _cacheStore.GetOrCreate(configId);
        var entries = ParseHistory(result.StandardOutput, normalizedKey, cache);

        return new GitFileHistoryDto(NormalizePath(normalizedKey), entries);
    }

    public async Task<GitCoChangeStatsDto> CoChangeStatsAsync(
        Guid configId,
        string repoRoot,
        string path,
        CancellationToken cancellationToken)
    {
        var history = await FileHistoryAsync(configId, repoRoot, path, cancellationToken);
        var cache = _cacheStore.GetOrCreate(configId);
        var targetKey = NormalizePathKey(path);

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in history.Entries)
        {
            if (!cache.CommitFiles.TryGetValue(entry.CommitSha, out var files))
            {
                continue;
            }

            foreach (var file in files)
            {
                if (string.Equals(file, targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                counts[file] = counts.TryGetValue(file, out var count) ? count + 1 : 1;
            }
        }

        var results = counts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .Select(kvp => new GitCoChangeEntryDto(NormalizePath(kvp.Key), kvp.Value))
            .ToList();

        return new GitCoChangeStatsDto(NormalizePath(targetKey), results);
    }

    private async Task<GitPathChangeDto?> GetLastChangeAsync(
        GitRepositoryCache cache,
        string repoRoot,
        string path,
        CancellationToken cancellationToken)
    {
        if (cache.PathMetadata.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[] { "log", "-n", "1", "--date=iso-strict", "--pretty=format:%H%x09%an%x09%ad", "--", path },
            cancellationToken);
        EnsureSuccess(result, "git log");

        var line = result.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Split('\t');
        if (parts.Length < 3)
        {
            return null;
        }

        var change = new GitPathChangeDto(parts[0], parts[1], DateTimeOffset.Parse(parts[2]));
        cache.PathMetadata[path] = change;
        return change;
    }

    private static List<GitFileHistoryEntryDto> ParseHistory(
        string output,
        string targetPath,
        GitRepositoryCache cache)
    {
        var entries = new List<GitFileHistoryEntryDto>();
        GitHistoryBuilder? current = null;

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (rawLine.StartsWith("COMMIT|", StringComparison.Ordinal))
            {
                if (current is not null)
                {
                    entries.Add(current.Build(targetPath, cache));
                }

                current = GitHistoryBuilder.FromHeader(rawLine);
                continue;
            }

            current?.AddChange(rawLine, targetPath, cache);
        }

        if (current is not null)
        {
            entries.Add(current.Build(targetPath, cache));
        }

        return entries;
    }

    private static bool MatchesNameFilter(string path, string? nameFilter)
    {
        if (string.IsNullOrWhiteSpace(nameFilter))
        {
            return true;
        }

        return path.Contains(nameFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesChangeFilters(GitPathChangeDto? change, DateTimeOffset? changedAfter, string? author)
    {
        if (changedAfter.HasValue)
        {
            if (change is null || change.CommitDateUtc <= changedAfter.Value)
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            if (change is null || !change.Author.Contains(author, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        var normalized = NormalizePathKey(path);
        return "./" + normalized;
    }

    private static string NormalizePathKey(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }

    private static void EnsureSuccess(GitCommandResult result, string commandName)
    {
        if (result.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? $"Git command failed: {commandName} (exit {result.ExitCode})."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }
    }

    private sealed class GitHistoryBuilder
    {
        private readonly string _commitSha;
        private readonly string _author;
        private readonly DateTimeOffset _date;
        private readonly string _message;
        private readonly List<GitFileChangeDto> _changes = new();
        private readonly HashSet<string> _commitFiles = new(StringComparer.OrdinalIgnoreCase);

        private GitHistoryBuilder(string commitSha, string author, DateTimeOffset date, string message)
        {
            _commitSha = commitSha;
            _author = author;
            _date = date;
            _message = message;
        }

        public static GitHistoryBuilder FromHeader(string header)
        {
            var parts = header.Split('|', 5);
            if (parts.Length < 5)
            {
                throw new InvalidOperationException("Unexpected git log header format.");
            }

            return new GitHistoryBuilder(parts[1], parts[3], DateTimeOffset.Parse(parts[2]), parts[4]);
        }

        public void AddChange(string line, string targetPath, GitRepositoryCache cache)
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
            {
                return;
            }

            var status = parts[0];
            var paths = new List<string>();

            if (status.StartsWith("R", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
            {
                paths.Add(parts[1]);
                paths.Add(parts[2]);
            }
            else
            {
                paths.Add(parts[1]);
            }

            foreach (var rawPath in paths)
            {
                var normalized = NormalizePathKey(rawPath);
                _commitFiles.Add(normalized);
                if (IsTargetPath(normalized, targetPath))
                {
                    _changes.Add(new GitFileChangeDto(NormalizePath(normalized), status, null, null));
                }
            }

            cache.CommitFiles[_commitSha] = _commitFiles;
        }

        public GitFileHistoryEntryDto Build(string targetPath, GitRepositoryCache cache)
        {
            cache.CommitFiles[_commitSha] = _commitFiles;
            return new GitFileHistoryEntryDto(_commitSha, _author, _date, _message, _changes.ToList());
        }

        private static bool IsTargetPath(string candidate, string targetPath)
        {
            return string.Equals(candidate, targetPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
