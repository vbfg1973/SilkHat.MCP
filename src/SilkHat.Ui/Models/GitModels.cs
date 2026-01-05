namespace SilkHat.Ui.Models;

public enum GitTreeEntryType
{
    File = 0,
    Directory = 1
}

public enum GitChangeKind
{
    Unknown = 0,
    Add = 1,
    Modify = 2,
    Delete = 3,
    Rename = 4,
    Copy = 5
}

public sealed record GitPathChangeModel(
    string CommitSha,
    string Author,
    DateTimeOffset CommitDateUtc);

public sealed record GitTreeEntryModel(
    string Path,
    string Name,
    GitTreeEntryType Type,
    GitPathChangeModel? LastChange);

public sealed record GitFileChangeModel(
    string Path,
    string Status,
    GitChangeKind ChangeKind,
    int? Additions,
    int? Deletions,
    int LinesBefore,
    int LinesAfter);

public sealed record GitFileHistoryEntryModel(
    string CommitSha,
    string Author,
    DateTimeOffset CommitDateUtc,
    string Message,
    IReadOnlyList<GitFileChangeModel> Changes);

public sealed record GitFileHistoryModel(
    string Path,
    IReadOnlyList<GitFileHistoryEntryModel> Entries);

public sealed record GitCoChangeEntryModel(
    string Path,
    int Count);

public sealed record GitCoChangeStatsModel(
    string Path,
    int TotalChangeCount,
    IReadOnlyList<GitCoChangeEntryModel> Entries);
