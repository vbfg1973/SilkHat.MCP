using Moq;
using SilkHat.Api.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;

namespace SilkHat.Api.Tests.Services;

public sealed class CodeTreeMetricsServiceTests
{
    [Fact]
    public async Task GetMetricsAsync_UsesGitMetrics_ForFileCounts()
    {
        var solution = BuildSolution();
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var gitMetrics = new GitFileMetricsSummary(
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 2,
                ["./Repo/Bar.cs"] = 5
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 1,
                ["./Repo/Bar.cs"] = 3
            },
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = new[] { "Alice" },
                ["./Repo/Bar.cs"] = new[] { "Alice", "Bob", "Cara" }
            });

        var gitAggregator = new Mock<IGitMetricsAggregator>();
        gitAggregator
            .Setup(service => service.GetMetricsAsync(It.IsAny<Guid>(), workspace.RootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMetrics);

        var complexityAggregator = new Mock<IComplexityMetricsAggregator>();
        var cacheStore = new CodeTreeMetricsCacheStore();
        var service = new CodeTreeMetricsService(cacheStore, complexityAggregator.Object, gitAggregator.Object);

        var metrics = await service.GetMetricsAsync(
            Guid.NewGuid(),
            workspace,
            solution,
            CodeTreeAnnotationKind.FileChangeCount,
            CancellationToken.None);

        Assert.Equal(2, metrics.FileValues["Repo/Foo/A.cs"]);
        Assert.Equal(7, metrics.NodeValues["Repo"]);
        Assert.Equal(2, metrics.NodeValues["Repo/Foo"]);
        Assert.Equal(5, metrics.NodeValues["Repo/Bar.cs"]);
    }

    [Fact]
    public async Task GetMetricsAsync_UsesDistinctAuthors_ForFolderCounts()
    {
        var solution = BuildSolution();
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var gitMetrics = new GitFileMetricsSummary(
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 1,
                ["./Repo/Bar.cs"] = 1
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 2,
                ["./Repo/Bar.cs"] = 2
            },
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = new[] { "Alice", "Bob" },
                ["./Repo/Bar.cs"] = new[] { "Alice", "Bob" }
            });

        var gitAggregator = new Mock<IGitMetricsAggregator>();
        gitAggregator
            .Setup(service => service.GetMetricsAsync(It.IsAny<Guid>(), workspace.RootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitMetrics);

        var complexityAggregator = new Mock<IComplexityMetricsAggregator>();
        var cacheStore = new CodeTreeMetricsCacheStore();
        var service = new CodeTreeMetricsService(cacheStore, complexityAggregator.Object, gitAggregator.Object);

        var metrics = await service.GetMetricsAsync(
            Guid.NewGuid(),
            workspace,
            solution,
            CodeTreeAnnotationKind.FileAuthorCount,
            CancellationToken.None);

        Assert.Equal(2, metrics.FileValues["Repo/Foo/A.cs"]);
        Assert.Equal(2, metrics.FileValues["Repo/Bar.cs"]);
        Assert.Equal(2, metrics.NodeValues["Repo"]);
        Assert.Equal(2, metrics.NodeValues["Repo/Foo"]);
    }

    [Fact]
    public async Task GetMetricsAsync_UsesComplexityMetrics_ForFileComplexity()
    {
        var solution = BuildSolution();
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var fileMetrics = new FileComplexityMetrics(
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 4
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 6
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["./Repo/Foo/A.cs"] = 8
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        var complexityAggregator = new Mock<IComplexityMetricsAggregator>();
        complexityAggregator
            .Setup(service => service.GetMetricsAsync(It.IsAny<Guid>(), workspace, solution, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileMetrics);

        var gitAggregator = new Mock<IGitMetricsAggregator>();
        var cacheStore = new CodeTreeMetricsCacheStore();
        var service = new CodeTreeMetricsService(cacheStore, complexityAggregator.Object, gitAggregator.Object);

        var metrics = await service.GetMetricsAsync(
            Guid.NewGuid(),
            workspace,
            solution,
            CodeTreeAnnotationKind.CyclomaticComplexity,
            CancellationToken.None);

        Assert.Equal(6, metrics.FileValues["Repo/Foo/A.cs"]);
        Assert.Equal(6, metrics.NodeValues["Repo/Foo"]);
    }

    private static CodeSolutionWorkspace BuildSolution()
    {
        var project = new CodeTreeEntryDto("./Repo", "Repo", "Repo", CodeTreeEntryType.Project, "alpha", "Repo", null, null, null, null, null, null);
        var folder = new CodeTreeEntryDto("./Repo/Foo", "Repo/Foo", "Foo", CodeTreeEntryType.Directory, "alpha", "Repo", null, null, null, null, null, null);
        var fileOne = new CodeTreeEntryDto("./Repo/Foo/A.cs", "Repo/Foo/A.cs", "A.cs", CodeTreeEntryType.File, "alpha", "Repo", null, null, null, null, null, null);
        var fileTwo = new CodeTreeEntryDto("./Repo/Bar.cs", "Repo/Bar.cs", "Bar.cs", CodeTreeEntryType.File, "alpha", "Repo", null, null, null, null, null, null);

        var entries = new List<CodeTreeEntryDto> { project, folder, fileOne, fileTwo };
        var children = new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new List<CodeTreeEntryDto> { project },
            ["Repo"] = new List<CodeTreeEntryDto> { folder, fileTwo },
            ["Repo/Foo"] = new List<CodeTreeEntryDto> { fileOne }
        };

        return new CodeSolutionWorkspace(
            "solution-1",
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>(),
            entries,
            children,
            new List<string>(),
            new List<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());
    }
}
