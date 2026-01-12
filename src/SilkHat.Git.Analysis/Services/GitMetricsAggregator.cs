using System.Collections.Concurrent;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;

namespace SilkHat.Git.Analysis.Services
{
    public sealed class GitMetricsAggregator : IGitMetricsAggregator
    {
        private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
        private readonly ConcurrentDictionary<Guid, Lazy<Task<GitFileMetricsSummary>>> _cache = new();
        private readonly IGitCommandRunner _runner;

        public GitMetricsAggregator(IGitCommandRunner runner)
        {
            _runner = runner;
        }

        public Task<GitFileMetricsSummary> GetMetricsAsync(
            Guid configId,
            string repoRoot,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
                throw new ArgumentException("Repository root path is required.", nameof(repoRoot));

            var lazy = _cache.GetOrAdd(
                configId,
                _ => new Lazy<Task<GitFileMetricsSummary>>(() => BuildMetricsAsync(repoRoot, cancellationToken)));

            return lazy.Value;
        }

        public void Invalidate(Guid configId)
        {
            _cache.TryRemove(configId, out _);
        }

        private async Task<GitFileMetricsSummary> BuildMetricsAsync(
            string repoRoot,
            CancellationToken cancellationToken)
        {
            var result = await _runner.ExecuteAsync(
                repoRoot,
                new[] { "log", "--numstat", "--pretty=format:COMMIT|%an", "--" },
                cancellationToken);

            if (result.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(result.StandardError)
                    ? "Git log failed while building metrics."
                    : result.StandardError.Trim();
                throw new InvalidOperationException(message);
            }

            var changeCounts = new Dictionary<string, int>(PathComparer);
            var authorSets = new Dictionary<string, HashSet<string>>(PathComparer);
            string? currentAuthor = null;

            var lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("COMMIT|", StringComparison.Ordinal))
                {
                    currentAuthor = line[7..].Trim();
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 3) continue;

                var rawPath = parts[2].Trim();
                if (string.IsNullOrWhiteSpace(rawPath)) continue;

                var normalizedPath = NormalizePath(ResolveRenamePath(rawPath));
                if (string.IsNullOrWhiteSpace(normalizedPath)) continue;

                if (changeCounts.TryGetValue(normalizedPath, out var existing))
                    changeCounts[normalizedPath] = existing + 1;
                else
                    changeCounts[normalizedPath] = 1;

                if (!string.IsNullOrWhiteSpace(currentAuthor))
                {
                    if (!authorSets.TryGetValue(normalizedPath, out var authors))
                    {
                        authors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        authorSets[normalizedPath] = authors;
                    }

                    authors.Add(currentAuthor);
                }
            }

            var authorCounts = authorSets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count, PathComparer);
            var authorsByFile = authorSets.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyCollection<string>)kvp.Value.ToList(),
                PathComparer);
            return new GitFileMetricsSummary(changeCounts, authorCounts, authorsByFile);
        }

        private static string ResolveRenamePath(string path)
        {
            if (!path.Contains("=>", StringComparison.Ordinal)) return path;

            if (path.Contains('{') && path.Contains('}'))
            {
                var open = path.IndexOf('{');
                var close = path.IndexOf('}');
                if (open >= 0 && close > open)
                {
                    var prefix = path[..open];
                    var suffix = path[(close + 1)..];
                    var segment = path[(open + 1)..close];
                    var parts = segment.Split("=>", 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var newSegment = parts[1].Trim();
                        return $"{prefix}{newSegment}{suffix}";
                    }
                }
            }

            var arrowIndex = path.LastIndexOf("=>", StringComparison.Ordinal);
            if (arrowIndex >= 0) return path[(arrowIndex + 2)..].Trim();

            return path;
        }

        private static string NormalizePathKey(string path)
        {
            var normalized = path.Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];

            return normalized;
        }

        private static string NormalizePath(string path)
        {
            var normalized = NormalizePathKey(path);
            return "./" + normalized;
        }
    }
}