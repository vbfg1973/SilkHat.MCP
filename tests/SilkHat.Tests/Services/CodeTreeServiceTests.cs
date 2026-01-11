using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class CodeTreeServiceTests
{
    [Fact]
    public async Task GetTreeAsync_DeduplicatesProjectsByNormalizedPath()
    {
        var solutionId = "solution-1";
        var provider = new GraphStoreProvider();
        var store = provider.GetOrAdd(solutionId);

        var solutionNode = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Solution, solutionId, "Solution");
        store.AddNode(solutionNode);

        var attrsA = new Dictionary<string, string>
        {
            ["DisplayPath"] = "./Repo",
            ["RepositoryPath"] = "./Repo",
            ["ProjectKey"] = "proj1",
            ["ProjectName"] = "Repo"
        };
        var projectA = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Project, "./Repo", "Repo", attrsA);

        var attrsB = new Dictionary<string, string>
        {
            ["DisplayPath"] = "Repo/",
            ["RepositoryPath"] = "Repo",
            ["ProjectKey"] = "proj1",
            ["ProjectName"] = "Repo"
        };
        var projectB = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Project, "Repo", "Repo", attrsB);

        store.AddNode(projectA);
        store.AddNode(projectB);
        store.AddEdge(solutionNode.Id, projectA.Id, EdgeType.Contains);
        store.AddEdge(solutionNode.Id, projectB.Id, EdgeType.Contains);

        var solution = new CodeSolutionWorkspace(
            solutionId,
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>
            {
                ["proj1"] = new ProjectIndex("proj1", "Repo", "C#", "Repo", Array.Empty<CodeProjectReferenceDto>(), Array.Empty<CodeProjectReferenceDto>())
            },
            Array.Empty<CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            Array.Empty<string>(),
            Array.Empty<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());

        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solutionId] = solution
        });

        var service = new CodeTreeService(provider);

        var children = await service.GetTreeAsync(workspace, solution, null, CancellationToken.None);

        Assert.Single(children);
        Assert.Equal("Repo", children[0].Name);
    }
}
