using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services
{
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
                new List<CodeTreeEntryDto>(),
                new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
                new List<string>(),
                new List<NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, Compilation>());
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
                new List<CodeTreeEntryDto>(),
                new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
                new List<string>(),
                new List<NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, Compilation>());
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
}