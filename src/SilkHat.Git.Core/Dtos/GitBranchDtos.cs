namespace SilkHat.Git.Core.Dtos;

public sealed record GitBranchListDto(
    string CurrentBranch,
    IReadOnlyList<string> LocalBranches);

