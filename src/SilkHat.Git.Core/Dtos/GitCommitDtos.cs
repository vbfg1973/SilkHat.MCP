namespace SilkHat.Git.Core.Dtos;

public sealed record GitCommitFileChangeDto(
    string Path,
    string? OldPath,
    string Status,
    GitChangeKind ChangeKind);

public sealed record GitCommitDto(
    string CommitSha,
    string AbbreviatedSha,
    IReadOnlyList<string> ParentShas,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset CommitDateUtc,
    string Subject,
    string Body,
    bool IsMerge,
    IReadOnlyList<GitCommitFileChangeDto> Changes);
