using SilkHat.Analysis.Services;

namespace SilkHat.Analysis.Models;

public sealed record RepoCommandContext(
    Guid ConfigId,
    string RootPath,
    LoadedRepositoryStore Store);
