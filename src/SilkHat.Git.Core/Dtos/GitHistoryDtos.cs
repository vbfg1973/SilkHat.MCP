namespace SilkHat.Git.Core.Dtos;

public sealed record GitFileChangeDto(
    string Path,
    string Status,
    int? Additions,
    int? Deletions);

public sealed record GitFileHistoryEntryDto(
    string CommitSha,
    string Author,
    DateTimeOffset CommitDateUtc,
    string Message,
    IReadOnlyList<GitFileChangeDto> Changes);

public sealed record GitFileHistoryDto(
    string Path,
    IReadOnlyList<GitFileHistoryEntryDto> Entries);

public sealed record GitCoChangeEntryDto(
    string Path,
    int Count);

public sealed record GitCoChangeStatsDto(
    string Path,
    IReadOnlyList<GitCoChangeEntryDto> Entries);
