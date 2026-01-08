using SilkHat.Core.Dtos;
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
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<GitCoChangeStatsDto> CoChangeStatsAsync(
        Guid configId,
        string repoRoot,
        string path,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<GitCommitDto>> QueryCommitsAsync(
        Guid configId,
        string repoRoot,
        string? author,
        string? sha,
        DateTimeOffset? since,
        DateTimeOffset? until,
        bool? merge,
        string? path,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<GitFileLastChangeDto> GetFileLastChangeAsync(
        Guid configId,
        string repoRoot,
        string path,
        bool includeDiff,
        CancellationToken cancellationToken);
}
