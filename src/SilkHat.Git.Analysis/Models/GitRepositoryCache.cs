using SilkHat.Git.Core.Dtos;

namespace SilkHat.Git.Analysis.Models;

public sealed class GitRepositoryCache
{
    public Dictionary<string, GitPathChangeDto> PathMetadata { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, HashSet<string>> CommitFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
}
