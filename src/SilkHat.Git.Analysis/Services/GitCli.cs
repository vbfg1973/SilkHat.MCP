using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;
using SilkHat.Core.Dtos;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Git.Analysis.Services;

public sealed class GitCli : IGitCli
{
    private const string CommitHeaderPrefix = "COMMIT|";
    private const string BodyBeginMarker = "BODY_BEGIN";
    private const string BodyEndMarker = "BODY_END";
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
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, PagingDefaults.MaxPageSize);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;

        var normalizedKey = NormalizePathKey(path);
        var enrichedEntries = await LoadHistoryEntriesAsync(configId, repoRoot, normalizedKey, cancellationToken);
        var totalCount = enrichedEntries.Count;
        var pageEntries = enrichedEntries
            .Skip(skip)
            .Take(normalizedPageSize)
            .ToList();

        var pagedEntries = new PagedResult<GitFileHistoryEntryDto>(
            pageEntries,
            normalizedPageNumber,
            normalizedPageSize,
            totalCount);

        return new GitFileHistoryDto(NormalizePath(normalizedKey), pagedEntries);
    }

    public async Task<GitCoChangeStatsDto> CoChangeStatsAsync(
        Guid configId,
        string repoRoot,
        string path,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, PagingDefaults.MaxPageSize);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;

        var cache = _cacheStore.GetOrCreate(configId);
        var targetKey = NormalizePathKey(path);
        var historyEntries = await LoadHistoryEntriesAsync(configId, repoRoot, targetKey, cancellationToken);

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in historyEntries)
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

        var ordered = counts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pageEntries = ordered
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(kvp => new GitCoChangeEntryDto(NormalizePath(kvp.Key), kvp.Value))
            .ToList();

        var pagedEntries = new PagedResult<GitCoChangeEntryDto>(
            pageEntries,
            normalizedPageNumber,
            normalizedPageSize,
            ordered.Count);

        return new GitCoChangeStatsDto(NormalizePath(targetKey), historyEntries.Count, pagedEntries);
    }

    private async Task<IReadOnlyList<GitFileHistoryEntryDto>> LoadHistoryEntriesAsync(
        Guid configId,
        string repoRoot,
        string normalizedKey,
        CancellationToken cancellationToken)
    {
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[] { "log", "--date=iso-strict", "--pretty=format:COMMIT|%H|%ad|%an|%s", "--name-status", "--", normalizedKey },
            cancellationToken);
        EnsureSuccess(result, "git log");

        var cache = _cacheStore.GetOrCreate(configId);
        var entries = ParseHistory(result.StandardOutput, normalizedKey, cache);
        return await PopulateHistoryMetricsAsync(repoRoot, entries, cancellationToken);
    }

    public async Task<PagedResult<GitCommitDto>> QueryCommitsAsync(
        Guid configId,
        string repoRoot,
        string? author,
        string? sha,
        DateTimeOffset? since,
        DateTimeOffset? until,
        bool? merge,
        string? path,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, PagingDefaults.MaxPageSize);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;
        var applyShaFilter = !string.IsNullOrWhiteSpace(sha);

        var args = new List<string>
        {
            "log",
            "--date=iso-strict",
            $"--pretty=format:{CommitHeaderPrefix}%H|%h|%P|%an|%ae|%ad|%s%n{BodyBeginMarker}%n%b%n{BodyEndMarker}",
            "--name-status"
        };

        if (!string.IsNullOrWhiteSpace(author))
        {
            args.Add($"--author={author}");
        }

        if (since.HasValue)
        {
            args.Add($"--since={since.Value:O}");
        }

        if (until.HasValue)
        {
            args.Add($"--until={until.Value:O}");
        }

        if (merge.HasValue)
        {
            args.Add(merge.Value ? "--merges" : "--no-merges");
        }

        var normalizedPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(path))
        {
            normalizedPath = NormalizePathKey(path);
            args.Add("--follow");
            args.Add("--find-renames");
            args.Add("--");
            args.Add(normalizedPath);
        }

        if (!applyShaFilter)
        {
            args.Add($"--skip={skip}");
            args.Add($"--max-count={normalizedPageSize}");
        }

        var result = await _runner.ExecuteAsync(repoRoot, args.ToArray(), cancellationToken);
        EnsureSuccess(result, "git log");

        var commits = GitCommitParser.Parse(result.StandardOutput).ToList();
        if (applyShaFilter)
        {
            var shaFilter = sha!.Trim();
            commits = commits
                .Where(commit =>
                    commit.CommitSha.StartsWith(shaFilter, StringComparison.OrdinalIgnoreCase) ||
                    commit.AbbreviatedSha.StartsWith(shaFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (applyShaFilter)
        {
            var paged = commits
                .Skip(skip)
                .Take(normalizedPageSize)
                .ToList();

            return new PagedResult<GitCommitDto>(paged, normalizedPageNumber, normalizedPageSize, commits.Count);
        }

        var totalCount = await CountCommitsAsync(
            repoRoot,
            author,
            since,
            until,
            merge,
            normalizedPath,
            cancellationToken);

        return new PagedResult<GitCommitDto>(commits, normalizedPageNumber, normalizedPageSize, totalCount);
    }

    public async Task<GitFileLastChangeDto> GetFileLastChangeAsync(
        Guid configId,
        string repoRoot,
        string path,
        bool includeDiff,
        CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizePathKey(path);
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[]
            {
                "log",
                "-n",
                "1",
                "--date=iso-strict",
                "--pretty=format:%H%x09%h%x09%an%x09%ae%x09%ad%x09%s",
                "--follow",
                "--",
                normalizedKey
            },
            cancellationToken);
        EnsureSuccess(result, "git log");

        var line = result.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new InvalidOperationException($"No commits found for '{normalizedKey}'.");
        }

        var parts = line.Split('\t');
        if (parts.Length < 6)
        {
            throw new InvalidOperationException($"Unable to parse last commit for '{normalizedKey}'.");
        }

        var commitSha = parts[0].Trim();
        var abbreviatedSha = parts[1].Trim();
        var author = parts[2].Trim();
        var authorEmail = parts[3].Trim();
        var date = DateTimeOffset.Parse(parts[4].Trim());
        var subject = parts[5].Trim();

        var diffLines = includeDiff
            ? await GetDiffLinesAsync(repoRoot, commitSha, normalizedKey, cancellationToken)
            : Array.Empty<GitFileDiffLineDto>();

        return new GitFileLastChangeDto(
            NormalizePath(normalizedKey),
            commitSha,
            abbreviatedSha,
            author,
            authorEmail,
            date,
            subject,
            diffLines);
    }

    private async Task<int> CountCommitsAsync(
        string repoRoot,
        string? author,
        DateTimeOffset? since,
        DateTimeOffset? until,
        bool? merge,
        string? normalizedPath,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(normalizedPath))
        {
            var args = new List<string> { "log", "--pretty=format:%H" };
            if (!string.IsNullOrWhiteSpace(author))
            {
                args.Add($"--author={author}");
            }

            if (since.HasValue)
            {
                args.Add($"--since={since.Value:O}");
            }

            if (until.HasValue)
            {
                args.Add($"--until={until.Value:O}");
            }

            if (merge.HasValue)
            {
                args.Add(merge.Value ? "--merges" : "--no-merges");
            }

            args.Add("--follow");
            args.Add("--find-renames");
            args.Add("--");
            args.Add(normalizedPath);

            var result = await _runner.ExecuteAsync(repoRoot, args.ToArray(), cancellationToken);
            EnsureSuccess(result, "git log");
            var lines = result.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lines.Length;
        }

        var countArgs = new List<string> { "rev-list", "--count", "HEAD" };
        if (!string.IsNullOrWhiteSpace(author))
        {
            countArgs.Add($"--author={author}");
        }

        if (since.HasValue)
        {
            countArgs.Add($"--since={since.Value:O}");
        }

        if (until.HasValue)
        {
            countArgs.Add($"--until={until.Value:O}");
        }

        if (merge.HasValue)
        {
            countArgs.Add(merge.Value ? "--merges" : "--no-merges");
        }

        var countResult = await _runner.ExecuteAsync(repoRoot, countArgs.ToArray(), cancellationToken);
        EnsureSuccess(countResult, "git rev-list");

        if (int.TryParse(countResult.StandardOutput.Trim(), out var total))
        {
            return total;
        }

        return 0;
    }

    private async Task<IReadOnlyList<GitFileDiffLineDto>> GetDiffLinesAsync(
        string repoRoot,
        string commitSha,
        string normalizedKey,
        CancellationToken cancellationToken)
    {
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[]
            {
                "show",
                commitSha,
                "--unified=0",
                "--format=",
                "--",
                normalizedKey
            },
            cancellationToken);
        EnsureSuccess(result, "git show");

        return ParseDiffLines(result.StandardOutput);
    }

    private static IReadOnlyList<GitFileDiffLineDto> ParseDiffLines(string output)
    {
        var lines = new List<GitFileDiffLineDto>();
        var currentLine = 1;

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            if (string.IsNullOrEmpty(rawLine))
            {
                continue;
            }

            if (rawLine.StartsWith("@@", StringComparison.Ordinal))
            {
                if (TryParseHunkStart(rawLine, out var newStart))
                {
                    currentLine = newStart;
                }
                continue;
            }

            if (rawLine.StartsWith("diff --git", StringComparison.Ordinal)
                || rawLine.StartsWith("index ", StringComparison.Ordinal)
                || rawLine.StartsWith("---", StringComparison.Ordinal)
                || rawLine.StartsWith("+++", StringComparison.Ordinal)
                || rawLine.StartsWith("\\ No newline", StringComparison.Ordinal))
            {
                continue;
            }

            if (rawLine.StartsWith('+'))
            {
                lines.Add(new GitFileDiffLineDto(currentLine, GitDiffLineKind.Add, rawLine[1..]));
                currentLine += 1;
                continue;
            }

            if (rawLine.StartsWith('-'))
            {
                lines.Add(new GitFileDiffLineDto(currentLine, GitDiffLineKind.Delete, rawLine[1..]));
                continue;
            }
        }

        return lines;
    }

    private static bool TryParseHunkStart(string line, out int newStart)
    {
        newStart = 1;
        var plusIndex = line.IndexOf('+');
        if (plusIndex < 0)
        {
            return false;
        }

        var index = plusIndex + 1;
        var value = 0;
        var hasDigit = false;
        while (index < line.Length && char.IsDigit(line[index]))
        {
            hasDigit = true;
            value = (value * 10) + (line[index] - '0');
            index += 1;
        }

        if (!hasDigit)
        {
            return false;
        }

        newStart = value;
        return true;
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

    private async Task<IReadOnlyList<GitFileHistoryEntryDto>> PopulateHistoryMetricsAsync(
        string repoRoot,
        IReadOnlyList<GitFileHistoryEntryDto> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return entries;
        }

        var enriched = new List<GitFileHistoryEntryDto>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry.Changes.Count == 0)
            {
                enriched.Add(entry);
                continue;
            }

            var changes = new List<GitFileChangeDto>(entry.Changes.Count);
            foreach (var change in entry.Changes)
            {
                var pathKey = NormalizePathKey(change.Path);
                var (additions, deletions) = await GetNumStatAsync(repoRoot, entry.CommitSha, pathKey, cancellationToken);

                var linesBefore = change.ChangeKind == GitChangeKind.Add
                    ? 0
                    : await GetFileLineCountAsync(repoRoot, $"{entry.CommitSha}^", pathKey, cancellationToken);

                var linesAfter = change.ChangeKind == GitChangeKind.Delete
                    ? 0
                    : await GetFileLineCountAsync(repoRoot, entry.CommitSha, pathKey, cancellationToken);

                changes.Add(change with
                {
                    Additions = additions,
                    Deletions = deletions,
                    LinesBefore = linesBefore,
                    LinesAfter = linesAfter
                });
            }

            enriched.Add(entry with { Changes = changes });
        }

        return enriched;
    }

    private async Task<(int? additions, int? deletions)> GetNumStatAsync(
        string repoRoot,
        string commitSha,
        string path,
        CancellationToken cancellationToken)
    {
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[] { "show", "--numstat", "--format=", commitSha, "--", path },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            return (null, null);
        }

        foreach (var line in result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3)
            {
                continue;
            }

            return (ParseNullableInt(parts[0]), ParseNullableInt(parts[1]));
        }

        return (null, null);
    }

    private async Task<int> GetFileLineCountAsync(
        string repoRoot,
        string commitSpec,
        string path,
        CancellationToken cancellationToken)
    {
        var result = await _runner.ExecuteAsync(
            repoRoot,
            new[] { "show", $"{commitSpec}:{path}" },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            return 0;
        }

        return CountLines(result.StandardOutput);
    }

    private static int? ParseNullableInt(string value)
    {
        if (value == "-" || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        var count = 1;
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                count++;
            }
        }

        if (content.EndsWith('\n'))
        {
            count--;
        }

        return count;
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
            var changeKind = ParseChangeKind(status);
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
                    _changes.Add(new GitFileChangeDto(NormalizePath(normalized), status, changeKind, null, null, 0, 0));
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

        private static GitChangeKind ParseChangeKind(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return GitChangeKind.Unknown;
            }

            return char.ToUpperInvariant(status[0]) switch
            {
                'A' => GitChangeKind.Add,
                'M' => GitChangeKind.Modify,
                'D' => GitChangeKind.Delete,
                'R' => GitChangeKind.Rename,
                'C' => GitChangeKind.Copy,
                _ => GitChangeKind.Unknown
            };
        }
    }
}

public static class GitCommitParser
{
    public static IReadOnlyList<GitCommitDto> Parse(string output)
    {
        var commits = new List<GitCommitDto>();
        GitCommitBuilder? current = null;

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            if (rawLine.StartsWith("COMMIT|", StringComparison.Ordinal))
            {
                if (current is not null)
                {
                    commits.Add(current.Build());
                }

                current = GitCommitBuilder.FromHeader(rawLine);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (string.Equals(rawLine, "BODY_BEGIN", StringComparison.Ordinal))
            {
                current.BeginBody();
                continue;
            }

            if (string.Equals(rawLine, "BODY_END", StringComparison.Ordinal))
            {
                current.EndBody();
                continue;
            }

            current.AddLine(rawLine);
        }

        if (current is not null)
        {
            commits.Add(current.Build());
        }

        return commits;
    }

    private sealed class GitCommitBuilder
    {
        private readonly string _commitSha;
        private readonly string _abbreviatedSha;
        private readonly IReadOnlyList<string> _parentShas;
        private readonly string _authorName;
        private readonly string _authorEmail;
        private readonly DateTimeOffset _date;
        private readonly string _subject;
        private readonly List<string> _bodyLines = new();
        private readonly List<GitCommitFileChangeDto> _changes = new();
        private bool _inBody;

        private GitCommitBuilder(
            string commitSha,
            string abbreviatedSha,
            IReadOnlyList<string> parentShas,
            string authorName,
            string authorEmail,
            DateTimeOffset date,
            string subject)
        {
            _commitSha = commitSha;
            _abbreviatedSha = abbreviatedSha;
            _parentShas = parentShas;
            _authorName = authorName;
            _authorEmail = authorEmail;
            _date = date;
            _subject = subject;
        }

        public static GitCommitBuilder FromHeader(string header)
        {
            var parts = header.Split('|', 8);
            if (parts.Length < 8)
            {
                throw new InvalidOperationException("Unexpected git log header format.");
            }

            var parents = parts[3]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            return new GitCommitBuilder(
                parts[1],
                parts[2],
                parents,
                parts[4],
                parts[5],
                DateTimeOffset.Parse(parts[6]),
                parts[7]);
        }

        public void BeginBody()
        {
            _inBody = true;
        }

        public void EndBody()
        {
            _inBody = false;
        }

        public void AddLine(string line)
        {
            if (_inBody)
            {
                _bodyLines.Add(line);
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            AddChange(line);
        }

        public GitCommitDto Build()
        {
            var body = string.Join('\n', _bodyLines).TrimEnd();
            var isMerge = _parentShas.Count > 1;

            return new GitCommitDto(
                _commitSha,
                _abbreviatedSha,
                _parentShas,
                _authorName,
                _authorEmail,
                _date,
                _subject,
                body,
                isMerge,
                _changes.ToList());
        }

        private void AddChange(string line)
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
            {
                return;
            }

            var status = parts[0];
            var changeKind = ParseChangeKind(status);

            if (status.StartsWith("R", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
            {
                _changes.Add(new GitCommitFileChangeDto(
                    NormalizePath(parts[2]),
                    NormalizePath(parts[1]),
                    status,
                    changeKind));
                return;
            }

            if (status.StartsWith("C", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
            {
                _changes.Add(new GitCommitFileChangeDto(
                    NormalizePath(parts[2]),
                    NormalizePath(parts[1]),
                    status,
                    changeKind));
                return;
            }

            _changes.Add(new GitCommitFileChangeDto(
                NormalizePath(parts[1]),
                null,
                status,
                changeKind));
        }
    }

    private static GitChangeKind ParseChangeKind(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return GitChangeKind.Unknown;
        }

        return char.ToUpperInvariant(status[0]) switch
        {
            'A' => GitChangeKind.Add,
            'M' => GitChangeKind.Modify,
            'D' => GitChangeKind.Delete,
            'R' => GitChangeKind.Rename,
            'C' => GitChangeKind.Copy,
            _ => GitChangeKind.Unknown
        };
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return "./" + normalized;
    }
}
