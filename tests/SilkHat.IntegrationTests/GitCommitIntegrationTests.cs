using SilkHat.Git.Analysis.Services;

namespace SilkHat.IntegrationTests;

public sealed class GitCommitIntegrationTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public async Task QueryCommits_ByAuthor_ReturnsKnownCommit()
    {
        var git = BuildGitCli();

        var commits = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            "Chris Russell",
            null,
            null,
            null,
            null,
            null,
            1,
            200,
            CancellationToken.None)).Items;

        Assert.Contains(commits, commit => commit.CommitSha.StartsWith("034d900", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryCommits_ByShortSha_ReturnsKnownCommit()
    {
        var git = BuildGitCli();

        var commits = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            null,
            "034d900",
            null,
            null,
            null,
            null,
            1,
            200,
            CancellationToken.None)).Items;

        Assert.Contains(commits, commit => commit.CommitSha.StartsWith("034d900", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryCommits_ByDateRange_ReturnsKnownCommit()
    {
        var git = BuildGitCli();
        var since = new DateTimeOffset(2026, 1, 7, 8, 49, 0, TimeSpan.Zero);
        var until = new DateTimeOffset(2026, 1, 7, 8, 50, 59, TimeSpan.Zero);

        var commits = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            null,
            null,
            since,
            until,
            null,
            null,
            1,
            200,
            CancellationToken.None)).Items;

        Assert.Contains(commits, commit => commit.CommitSha.StartsWith("034d900", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryCommits_MergeAndOrdinaryFilters_ReturnExpectedCommits()
    {
        var git = BuildGitCli();

        var merges = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            null,
            null,
            null,
            null,
            true,
            null,
            1,
            200,
            CancellationToken.None)).Items;

        Assert.Contains(merges, commit => commit.CommitSha.StartsWith("284498", StringComparison.OrdinalIgnoreCase));

        var ordinary = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            null,
            null,
            null,
            null,
            false,
            null,
            1,
            200,
            CancellationToken.None)).Items;

        Assert.Contains(ordinary, commit => commit.CommitSha.StartsWith("034d900", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryCommits_ByFilePath_ReturnsKnownCommit()
    {
        var git = BuildGitCli();

        var commits = (await git.QueryCommitsAsync(
            Guid.NewGuid(),
            RepoRoot,
            null,
            null,
            null,
            null,
            null,
            "docs/README.md",
            1,
            200,
            CancellationToken.None)).Items;

        var commit = commits.FirstOrDefault(item => item.CommitSha.StartsWith("034d900", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(commit);
        Assert.Contains(commit!.Changes, change => change.Path == "./docs/README.md");
    }

    [Fact]
    public async Task GetCurrentBranch_ReturnsBranchName()
    {
        var git = BuildGitCli();

        var branch = await git.GetCurrentBranchAsync(Guid.NewGuid(), RepoRoot, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(branch));
    }

    [Fact]
    public async Task ListLocalBranches_IncludesCurrentBranch()
    {
        var git = BuildGitCli();

        var current = await git.GetCurrentBranchAsync(Guid.NewGuid(), RepoRoot, CancellationToken.None);
        var branches = await git.ListLocalBranchesAsync(Guid.NewGuid(), RepoRoot, CancellationToken.None);

        Assert.Contains(current, branches);
    }

    [Fact]
    public async Task GetFileChangeCount_ReturnsPositiveCount()
    {
        var git = BuildGitCli();

        var count = await git.GetFileChangeCountAsync(
            Guid.NewGuid(),
            RepoRoot,
            "docs/README.md",
            CancellationToken.None);

        Assert.True(count.ChangeCount > 0);
    }

    private static GitCli BuildGitCli()
    {
        return new GitCli(new GitCommandRunner(), new GitRepositoryCacheStore());
    }

    private static string ResolveRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetFullPath(Path.Combine(baseDir, "../../../../.."));
        return root;
    }
}
