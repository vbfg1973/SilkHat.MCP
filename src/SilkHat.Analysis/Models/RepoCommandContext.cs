using SilkHat.Analysis.Abstractions;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Analysis.Models
{
    public sealed record RepoCommandContext(
        Guid ConfigId,
        string RootPath,
        IReadOnlyList<SolutionReference> Solutions,
        ILoadedRepositoryStore Store,
        ICodeWorkspaceStore CodeWorkspaceStore,
        ICodeWorkspaceLoader CodeWorkspaceLoader);
}