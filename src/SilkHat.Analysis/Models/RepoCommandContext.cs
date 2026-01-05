using SilkHat.Analysis.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Analysis.Models;

public sealed record RepoCommandContext(
    Guid ConfigId,
    string RootPath,
    LoadedRepositoryStore Store,
    CodeWorkspaceStore CodeWorkspaceStore,
    ICodeWorkspaceLoader CodeWorkspaceLoader);
