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
A	src/Program.cs
COMMIT|sha2|2024-01-02T00:00:00Z|bob|Update
M	src/Program.cs
""");
        runner.Add("show --numstat --format= sha1 -- src/Program.cs", "2\t0\tsrc/Program.cs");
        runner.Add("show --numstat --format= sha2 -- src/Program.cs", "1\t0\tsrc/Program.cs");
        runner.Add("show sha1:src/Program.cs", "line1\nline2\n");
        runner.Add("show sha2^:src/Program.cs", "line1\nline2\n");
        runner.Add("show sha2:src/Program.cs", "line1\nline2\nline3\n");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var history = await cli.FileHistoryAsync(Guid.NewGuid(), "/repo", "src/Program.cs", 1, 50, CancellationToken.None);

        Assert.Equal("./src/Program.cs", history.Path);
        Assert.Equal(2, history.Entries.TotalCount);
        Assert.Equal("sha1", history.Entries.Items[0].CommitSha);
        Assert.Equal(GitChangeKind.Add, history.Entries.Items[0].Changes[0].ChangeKind);
        Assert.Equal(0, history.Entries.Items[0].Changes[0].LinesBefore);
        Assert.Equal(2, history.Entries.Items[0].Changes[0].LinesAfter);
        Assert.Equal(GitChangeKind.Modify, history.Entries.Items[1].Changes[0].ChangeKind);
        Assert.Equal(2, history.Entries.Items[1].Changes[0].LinesBefore);
        Assert.Equal(3, history.Entries.Items[1].Changes[0].LinesAfter);
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
        runner.Add("show --numstat --format= sha1 -- src/Program.cs", "1\t1\tsrc/Program.cs");
        runner.Add("show sha1^:src/Program.cs", "line1\n");
        runner.Add("show sha1:src/Program.cs", "line1\nline2\n");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var stats = await cli.CoChangeStatsAsync(Guid.NewGuid(), "/repo", "src/Program.cs", 1, 50, CancellationToken.None);

        Assert.Equal(1, stats.TotalChangeCount);
        Assert.Single(stats.Entries.Items);
        Assert.Equal("./src/Other.cs", stats.Entries.Items[0].Path);
        Assert.Equal(1, stats.Entries.Items[0].Count);
    }

    [Fact]
    public async Task GetFileLastChangeAsync_ParsesDiffLines()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("log -n 1 --date=iso-strict --pretty=format:%H%x09%h%x09%an%x09%ae%x09%ad%x09%s --follow -- src/Program.cs",
            "sha1\tsha1\talice\talice@example.com\t2024-01-01T00:00:00Z\tUpdate");
        runner.Add("show sha1 --unified=0 --format= -- src/Program.cs", """
@@ -1,1 +1,2 @@
-line1
+line1 updated
+line2
""");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var change = await cli.GetFileLastChangeAsync(Guid.NewGuid(), "/repo", "src/Program.cs", true, CancellationToken.None);

        Assert.Equal("sha1", change.CommitSha);
        Assert.Equal("sha1", change.AbbreviatedSha);
        Assert.Equal("alice", change.Author);
        Assert.Equal("alice@example.com", change.AuthorEmail);
        Assert.Equal(3, change.DiffLines.Count);
        Assert.Equal(GitDiffLineKind.Delete, change.DiffLines[0].Kind);
        Assert.Equal(1, change.DiffLines[0].LineNumber);
        Assert.Equal(GitDiffLineKind.Add, change.DiffLines[1].Kind);
        Assert.Equal(1, change.DiffLines[1].LineNumber);
        Assert.Equal(GitDiffLineKind.Add, change.DiffLines[2].Kind);
        Assert.Equal(2, change.DiffLines[2].LineNumber);
    }

    [Fact]
    public async Task GetFileChangeCountAsync_ReturnsCommitCount()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("log --follow --pretty=format:%H -- src/Program.cs", "sha1\nsha2\n");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var count = await cli.GetFileChangeCountAsync(Guid.NewGuid(), "/repo", "src/Program.cs", CancellationToken.None);

        Assert.Equal("./src/Program.cs", count.Path);
        Assert.Equal(2, count.ChangeCount);
    }

    [Fact]
    public async Task GetCurrentBranchAsync_ReturnsBranchName()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("rev-parse --abbrev-ref HEAD", "feature/demo\n");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var branch = await cli.GetCurrentBranchAsync(Guid.NewGuid(), "/repo", CancellationToken.None);

        Assert.Equal("feature/demo", branch);
    }

    [Fact]
    public async Task ListLocalBranchesAsync_ReturnsSortedBranches()
    {
        var runner = new FakeGitCommandRunner();
        runner.Add("for-each-ref refs/heads --format=%(refname:short)", "master\nfeature/demo\ndevelop\n");

        var cacheStore = new GitRepositoryCacheStore();
        var cli = new GitCli(runner, cacheStore);

        var branches = await cli.ListLocalBranchesAsync(Guid.NewGuid(), "/repo", CancellationToken.None);

        Assert.Equal(new[] { "develop", "feature/demo", "master" }, branches);
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
