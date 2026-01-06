using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class CodeWorkspaceStoreTests
{
    [Fact]
    public void SetThenGet_ReturnsWorkspace()
    {
        var store = new CodeWorkspaceStore();
        var id = Guid.NewGuid();
        var solution = new CodeSolutionWorkspace(
            "solution-1",
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());
        var workspace = new CodeRepositoryWorkspace(
            "/repo",
            new Dictionary<string, CodeSolutionWorkspace>
            {
                [solution.SolutionId] = solution
            });

        store.Set(id, workspace);

        Assert.Same(workspace, store.Get(id));
    }

    [Fact]
    public void Remove_DeletesWorkspace()
    {
        var store = new CodeWorkspaceStore();
        var id = Guid.NewGuid();
        var solution = new CodeSolutionWorkspace(
            "solution-1",
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());
        var workspace = new CodeRepositoryWorkspace(
            "/repo",
            new Dictionary<string, CodeSolutionWorkspace>
            {
                [solution.SolutionId] = solution
            });

        store.Set(id, workspace);

        var removed = store.Remove(id);

        Assert.True(removed);
        Assert.Null(store.Get(id));
    }
}
