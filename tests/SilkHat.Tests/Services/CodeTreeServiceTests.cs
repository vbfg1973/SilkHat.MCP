using Moq;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class CodeTreeServiceTests
{
    [Fact]
    public async Task GetTree_ReturnsProjects_WhenParentIdIsNull()
    {
        var provider = BuildGraphProvider();
        var service = new CodeTreeService(provider);
        var solution = BuildSolutionWorkspace(provider);
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var results = await service.GetTreeAsync(workspace, solution, null, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(CodeTreeEntryType.Project, results[0].Type);
        Assert.Equal("Repo", results[0].Name);
    }

    [Fact]
    public async Task GetTree_ReturnsDirectChildren_WhenParentIdProvided()
    {
        var provider = BuildGraphProvider();
        var service = new CodeTreeService(provider);
        var solution = BuildSolutionWorkspace(provider);
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solution.SolutionId] = solution
        });

        var results = await service.GetTreeAsync(workspace, solution, "Repo", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(CodeTreeEntryType.Directory, results[0].Type);
        Assert.Equal(CodeTreeEntryType.File, results[1].Type);
        Assert.Contains(results, entry => entry.Name == "src" && entry.Type == CodeTreeEntryType.Directory);
        Assert.Contains(results, entry => entry.Name == "Program.cs" && entry.Type == CodeTreeEntryType.File);
    }

    private static IGraphStoreProvider BuildGraphProvider()
    {
        return new GraphStoreProvider();
    }

    private static CodeSolutionWorkspace BuildSolutionWorkspace(IGraphStoreProvider provider)
    {
        var entries = new List<CodeTreeEntryDto>
        {
            new("./Repo", "Repo", "Repo", CodeTreeEntryType.Project, "repo", "Repo", null, null, null, null, null, null),
            new("./Repo/src", "Repo/src", "src", CodeTreeEntryType.Directory, "repo", "Repo", null, null, null, null, null, null),
            new("./Repo/src/Nested", "Repo/src/Nested", "Nested", CodeTreeEntryType.Directory, "repo", "Repo", null, null, null, null, null, null),
            new("./Repo/src/Program.cs", "Repo/src/Program.cs", "Program.cs", CodeTreeEntryType.File, "repo", "Repo", null, null, null, null, null, null),
            new("./Repo/Program.cs", "Repo/Program.cs", "Program.cs", CodeTreeEntryType.File, "repo", "Repo", null, null, null, null, null, null),
            new("./Repo/src/Nested/Thing.cs", "Repo/src/Nested/Thing.cs", "Thing.cs", CodeTreeEntryType.File, "repo", "Repo", null, null, null, null, null, null)
        };
        var treeMap = BuildTreeChildrenMap(entries);

        var solution = new CodeSolutionWorkspace(
            "solution-1",
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>(),
            entries,
            treeMap,
            new List<string>(),
            new List<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());

        SeedGraph(provider, solution, entries, treeMap);
        return solution;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> BuildTreeChildrenMap(
        IReadOnlyList<CodeTreeEntryDto> entries)
    {
        var map = new Dictionary<string, List<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var parentKey = string.Empty;
            if (entry.Type != CodeTreeEntryType.Project)
            {
                var lastSeparator = entry.DisplayPath.LastIndexOf('/');
                parentKey = lastSeparator <= 0 ? string.Empty : entry.DisplayPath[..lastSeparator];
            }
            if (!map.TryGetValue(parentKey, out var children))
            {
                children = new List<CodeTreeEntryDto>();
                map[parentKey] = children;
            }

            children.Add(entry);
        }

        return map.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<CodeTreeEntryDto>)item.Value
                .OrderBy(entry => entry.Type == CodeTreeEntryType.Directory ? 0 : 1)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
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
