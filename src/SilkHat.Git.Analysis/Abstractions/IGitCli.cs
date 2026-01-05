using SilkHat.Git.Core.Dtos;

namespace SilkHat.Git.Analysis.Abstractions;

public interface IGitCli
{
    Task<IReadOnlyList<GitTreeEntryDto>> ListTreeAsync(
        Guid configId,
        string repoRoot,
        string? nameFilter,
        GitTreeEntryType? typeFilter,
        DateTimeOffset? changedAfter,
        string? author,
        CancellationToken cancellationToken);

    Task<GitFileHistoryDto> FileHistoryAsync(
        Guid configId,
        string repoRoot,
        string path,
        CancellationToken cancellationToken);

    Task<GitCoChangeStatsDto> CoChangeStatsAsync(
        Guid configId,
        string repoRoot,
        string path,
        CancellationToken cancellationToken);
}
