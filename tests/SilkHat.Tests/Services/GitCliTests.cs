using SilkHat.Git.Analysis.Services;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class GitCliTests
{
    [Fact]
    public async Task ListTreeAsync_IncludesDirectoriesAndFiles()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("ls-tree -r --name-only HEAD", "src/Program.cs\nsrc/Sub/Util.cs\n");
        runner.Add("log -n 1 --date=iso-strict --pretty=format:%H%x09%an%x09%ad -- src/Program.cs",
            "sha1\talice\t2024-01-01T00:00:00Z");
        runner.Add("log -n 1 --date=iso-strict --pretty=format:%H%x09%an%x09%ad -- src/Sub/Util.cs",
            "sha2\tbob\t2024-01-02T00:00:00Z");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var entries = await cli.ListTreeAsync(Guid.NewGuid(), "/repo", null, null, null, null, CancellationToken.None);

        Assert.Contains(entries, entry => entry.Type == GitTreeEntryType.Directory && entry.Path == "./src");
        Assert.Contains(entries, entry => entry.Type == GitTreeEntryType.Directory && entry.Path == "./src/Sub");
        Assert.Contains(entries, entry => entry.Path == "./src/Program.cs");
        Assert.Contains(entries, entry => entry.Path == "./src/Sub/Util.cs");
    }

    [Fact]
    public async Task FileHistoryAsync_ParsesCommits()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("log --date=iso-strict --pretty=format:COMMIT|%H|%ad|%an|%s --name-status -- src/Program.cs", """
COMMIT|sha1|2024-01-01T00:00:00Z|alice|Init
M	src/Program.cs
COMMIT|sha2|2024-01-02T00:00:00Z|bob|Update
M	src/Program.cs
""");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var history = await cli.FileHistoryAsync(Guid.NewGuid(), "/repo", "src/Program.cs", CancellationToken.None);

        Assert.Equal("./src/Program.cs", history.Path);
        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("sha1", history.Entries[0].CommitSha);
    }

    [Fact]
    public async Task CoChangeStatsAsync_CountsOtherFiles()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("log --date=iso-strict --pretty=format:COMMIT|%H|%ad|%an|%s --name-status -- src/Program.cs", """
COMMIT|sha1|2024-01-01T00:00:00Z|alice|Init
M	src/Program.cs
M	src/Other.cs
M	src/Other.cs
""");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var stats = await cli.CoChangeStatsAsync(Guid.NewGuid(), "/repo", "src/Program.cs", CancellationToken.None);

        Assert.Single(stats.Entries);
        Assert.Equal("./src/Other.cs", stats.Entries[0].Path);
        Assert.Equal(1, stats.Entries[0].Count);
    }

    private sealed class FakeGitCommandRunner : IGitCommandRunner
    {
        private readonly Dictionary<string, string> _responses = new(StringComparer.Ordinal);

        public void Add(string args, string output)
        {
            _responses[args] = output;
        }

        public Task<GitCommandResult> ExecuteAsync(string repoRoot, string[] args, CancellationToken cancellationToken)
        {
            var key = string.Join(" ", args);
            if (_responses.TryGetValue(key, out var output))
            {
                return Task.FromResult(new GitCommandResult(0, output, string.Empty));
            }

            return Task.FromResult(new GitCommandResult(1, string.Empty, "git error"));
        }
    }
}
