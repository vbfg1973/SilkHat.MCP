using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class GitMetricsAggregatorTests
{
    [Fact]
    public async Task GetMetricsAsync_AggregatesCountsAndAuthors()
    {
        var output = string.Join('\n', new[]
        {
            "COMMIT|Alice",
            "1\t0\tsrc/Foo.cs",
            "2\t1\tsrc/Bar.cs",
            "COMMIT|Bob",
            "5\t2\tsrc/Foo.cs"
        });

        var runner = new FakeRunner(new GitCommandResult(0, output, string.Empty));
        var aggregator = new GitMetricsAggregator(runner);

        var summary = await aggregator.GetMetricsAsync(Guid.NewGuid(), "/repo", CancellationToken.None);

        Assert.Equal(2, summary.ChangeCounts["./src/Foo.cs"]);
        Assert.Equal(1, summary.ChangeCounts["./src/Bar.cs"]);
        Assert.Equal(2, summary.AuthorCounts["./src/Foo.cs"]);
        Assert.Equal(1, summary.AuthorCounts["./src/Bar.cs"]);
        Assert.Contains("Alice", summary.AuthorsByFile["./src/Foo.cs"]);
        Assert.Contains("Bob", summary.AuthorsByFile["./src/Foo.cs"]);
    }

    private sealed class FakeRunner : IGitCommandRunner
    {
        private readonly GitCommandResult _result;

        public FakeRunner(GitCommandResult result)
        {
            _result = result;
        }

        public Task<GitCommandResult> ExecuteAsync(string repoRoot, string[] args, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }
}
