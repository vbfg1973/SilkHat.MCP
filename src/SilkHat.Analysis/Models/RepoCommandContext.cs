using SilkHat.Analysis.Abstractions;
using SilkHat.Code.Analysis.Abstractions;

namespace SilkHat.Analysis.Models;

public sealed record RepoCommandContext(
    Guid ConfigId,
    string RootPath,
    ILoadedRepositoryStore Store,
    ICodeWorkspaceStore CodeWorkspaceStore,
    ICodeWorkspaceLoader CodeWorkspaceLoader);
