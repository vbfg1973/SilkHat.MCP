using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;
using SilkHat.Git.Analysis.Services;

namespace SilkHat.Tests.Services
{
    public sealed class GitMetricsHarnessTests
    {
        [Fact]
        public async Task Parses_Log_And_Aggregates_File_Metrics()
        {
            var log = @"
COMMIT|sha1|sha1||alice|alice@test|2025-01-01T00:00:00Z|first
M	src/FileA.cs
M	src/FileB.cs
COMMIT|sha2|sha2||bob|bob@test|2025-01-02T00:00:00Z|second
M	src/FileA.cs
M	src/FileC.cs
COMMIT|sha3|sha3||alice|alice@test|2025-01-03T00:00:00Z|third
M	src/FileA.cs
";

            var runner = new FakeGitCommandRunner(log);
            var cli = new GitCli(runner, new InMemoryGitRepositoryCacheStore());

            var paged = await cli.QueryCommitsAsync(
                Guid.NewGuid(),
                "/repo",
                null,
                null,
                null,
                null,
                null,
                null,
                1,
                100,
                default);

            var commits = paged.Items;
            Assert.Equal(3, commits.Count);

            // Distinct authors
            var authors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var commit in commits) authors.Add(commit.AuthorEmail ?? commit.AuthorName);
            Assert.Equal(2, authors.Count);

            // File aggregates
            var fileAChanges = 0;
            var fileAAuthorEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var commit in commits)
            foreach (var change in commit.Changes)
                if (IsPath(change.Path, "src/FileA.cs"))
                {
                    fileAChanges++;
                    fileAAuthorEmails.Add(commit.AuthorEmail ?? commit.AuthorName);
                }

            // FileA touched in 3 commits, by 2 distinct authors
            Assert.Equal(3, fileAChanges);
            Assert.Equal(2, fileAAuthorEmails.Count);
        }

        private static bool IsPath(string? candidate, string expected)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return false;

            var normalized = candidate.TrimStart('.', '/');
            return string.Equals(normalized, expected.TrimStart('/'), StringComparison.OrdinalIgnoreCase);
        }

        private sealed class FakeGitCommandRunner : IGitCommandRunner
        {
            private readonly string _log;

            public FakeGitCommandRunner(string log)
            {
                _log = log;
            }

            public Task<GitCommandResult> ExecuteAsync(string repoRoot, string[] args,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new GitCommandResult(0, _log, string.Empty));
            }
        }

        private sealed class InMemoryGitRepositoryCacheStore : IGitRepositoryCacheStore
        {
            private readonly Dictionary<Guid, GitRepositoryCache> _cache = new();

            public GitRepositoryCache GetOrCreate(Guid configId)
            {
                if (!_cache.TryGetValue(configId, out var cache))
                {
                    cache = new GitRepositoryCache();
                    _cache[configId] = cache;
                }

                return cache;
            }

            public bool Remove(Guid configId)
            {
                return _cache.Remove(configId);
            }
        }
    }
}