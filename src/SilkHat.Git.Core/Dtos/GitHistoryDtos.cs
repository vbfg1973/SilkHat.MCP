using SilkHat.Core.Dtos;

namespace SilkHat.Git.Core.Dtos;

public enum GitChangeKind
{
    Unknown = 0,
    Add = 1,
    Modify = 2,
    Delete = 3,
    Rename = 4,
    Copy = 5
}

public sealed record GitFileChangeDto(
    string Path,
    string Status,
    GitChangeKind ChangeKind,
    int? Additions,
    int? Deletions,
    int LinesBefore,
    int LinesAfter);

public sealed record GitFileHistoryEntryDto(
    string CommitSha,
    string Author,
    DateTimeOffset CommitDateUtc,
    string Message,
    IReadOnlyList<GitFileChangeDto> Changes);

public sealed record GitFileHistoryDto(
    string Path,
    PagedResult<GitFileHistoryEntryDto> Entries);

public sealed record GitCoChangeEntryDto(
    string Path,
    int Count);

public sealed record GitCoChangeStatsDto(
    string Path,
    int TotalChangeCount,
    PagedResult<GitCoChangeEntryDto> Entries);
