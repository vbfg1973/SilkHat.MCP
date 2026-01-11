using Moq;
using SilkHat.Api.Models;
using SilkHat.Api.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Services;

public sealed class CodeTreeQueryServiceTests
{
    [Fact]
    public async Task GetTreeAsync_FiltersChildrenByMetric()
    {
        var provider = new GraphStoreProvider();
        var solution = BuildSolution(provider);
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var metrics = new CodeTreeMetricValues(
            CodeTreeAnnotationKind.CyclomaticComplexity,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo/Foo/A.cs"] = 10,
                ["Repo/Bar.cs"] = 1
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo"] = 11,
                ["Repo/Foo"] = 10,
                ["Repo/Foo/A.cs"] = 10,
                ["Repo/Bar.cs"] = 1
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        var metricsService = new Mock<ICodeTreeMetricsService>();
        metricsService.Setup(service => service.GetMetricsAsync(
                It.IsAny<Guid>(),
                workspace,
                solution,
                CodeTreeAnnotationKind.CyclomaticComplexity,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var service = new CodeTreeQueryService(
            new CodeTreeService(provider),
            metricsService.Object,
            new GraphQueryService(provider));

        var query = new CodeTreeQuery
        {
            ParentId = "Repo",
            FilterMetric = CodeTreeAnnotationKind.CyclomaticComplexity,
            FilterOperator = CodeTreeFilterOperator.GreaterThan,
            FilterThreshold = 5
        };

        var result = await service.GetTreeAsync(Guid.NewGuid(), workspace, solution, query, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Repo/Foo", result.Items[0].DisplayPath);
    }

    [Fact]
    public async Task GetTreeAsync_AnnotatesEntries()
    {
        var provider = new GraphStoreProvider();
        var solution = BuildSolution(provider);
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var metrics = new CodeTreeMetricValues(
            CodeTreeAnnotationKind.IndentationComplexity,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo/Foo/A.cs"] = 3
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo"] = 3,
                ["Repo/Foo"] = 3,
                ["Repo/Foo/A.cs"] = 3,
                ["Repo/Bar.cs"] = 0
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        var metricsService = new Mock<ICodeTreeMetricsService>();
        metricsService.Setup(service => service.GetMetricsAsync(
                It.IsAny<Guid>(),
                workspace,
                solution,
                CodeTreeAnnotationKind.IndentationComplexity,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var service = new CodeTreeQueryService(
            new CodeTreeService(provider),
            metricsService.Object,
            new GraphQueryService(provider));

        var query = new CodeTreeQuery
        {
            ParentId = null,
            AnnotationKind = CodeTreeAnnotationKind.IndentationComplexity
        };

        var result = await service.GetTreeAsync(Guid.NewGuid(), workspace, solution, query, CancellationToken.None);

        var entry = Assert.Single(result.Items);
        Assert.Equal(CodeTreeAnnotationKind.IndentationComplexity, entry.AnnotationKind);
        Assert.Equal(3, entry.AnnotationValue);
    }

    [Fact]
    public async Task GetTreeAsync_AnnotatesChildren_WhenParentSpecified()
    {
        var provider = new GraphStoreProvider();
        var solution = BuildSolution(provider);
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var metrics = new CodeTreeMetricValues(
            CodeTreeAnnotationKind.CyclomaticComplexity,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo/Foo/A.cs"] = 4
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Repo"] = 4,
                ["Repo/Foo"] = 4,
                ["Repo/Foo/A.cs"] = 4,
                ["Repo/Bar.cs"] = 0
            },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        var metricsService = new Mock<ICodeTreeMetricsService>();
        metricsService.Setup(service => service.GetMetricsAsync(
                It.IsAny<Guid>(),
                workspace,
                solution,
                CodeTreeAnnotationKind.CyclomaticComplexity,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var service = new CodeTreeQueryService(
            new CodeTreeService(provider),
            metricsService.Object,
            new GraphQueryService(provider));

        var query = new CodeTreeQuery
        {
            ParentId = "Repo",
            AnnotationKind = CodeTreeAnnotationKind.CyclomaticComplexity
        };

        var result = await service.GetTreeAsync(Guid.NewGuid(), workspace, solution, query, CancellationToken.None);

        Assert.Contains(result.Items, entry => entry.DisplayPath == "Repo/Foo");
        Assert.Contains(result.Items, entry => entry.DisplayPath == "Repo/Bar.cs");
        Assert.All(result.Items, entry => Assert.Equal(CodeTreeAnnotationKind.CyclomaticComplexity, entry.AnnotationKind));
    }

    private static CodeSolutionWorkspace BuildSolution(IGraphStoreProvider provider)
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

        var solution = new CodeSolutionWorkspace(
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

        SeedGraph(provider, solution, entries, children);
        return solution;
    }

    private static void SeedGraph(
        IGraphStoreProvider provider,
        CodeSolutionWorkspace solution,
        IReadOnlyList<CodeTreeEntryDto> entries,
        IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> children)
    {
        var store = provider.GetOrAdd(solution.SolutionId);
        var solutionNode = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Solution, solution.SolutionId, solution.SolutionName);
        store.AddNode(solutionNode);

        var nodeByPath = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [solution.SolutionId] = solutionNode.Id
        };

        foreach (var entry in entries)
        {
            var kind = entry.Type switch
            {
                CodeTreeEntryType.Project => GraphNodeKind.Project,
                CodeTreeEntryType.Directory => GraphNodeKind.Folder,
                CodeTreeEntryType.File => GraphNodeKind.File,
                _ => GraphNodeKind.File
            };
            var attrs = new Dictionary<string, string>
            {
                ["DisplayPath"] = entry.DisplayPath
            };
            if (entry.Type is CodeTreeEntryType.File or CodeTreeEntryType.Directory)
            {
                attrs["RepositoryPath"] = entry.RepositoryPath;
            }
            var nodeId = Guid.NewGuid();
            store.AddNode(new GraphNodeDto(nodeId, kind, entry.DisplayPath, entry.Name, attrs));
            nodeByPath[entry.DisplayPath] = nodeId;
        }

        foreach (var (parent, kids) in children)
        {
            var parentId = nodeByPath.TryGetValue(parent, out var id) ? id : solutionNode.Id;
            foreach (var child in kids)
            {
                if (nodeByPath.TryGetValue(child.DisplayPath, out var childId))
                {
                    store.AddEdge(parentId, childId, EdgeType.Contains);
                }
            }
        }
    }
}
