namespace SilkHat.Git.Core.Dtos
{
    public enum GitDiffLineKind
    {
        Add = 0,
        Delete = 1
    }

    public sealed record GitFileDiffLineDto(
        int LineNumber,
        GitDiffLineKind Kind,
        string Content);

    public sealed record GitFileLastChangeDto(
        string Path,
        string CommitSha,
        string AbbreviatedSha,
        string Author,
        string AuthorEmail,
        DateTimeOffset CommitDateUtc,
        string Subject,
        IReadOnlyList<GitFileDiffLineDto> DiffLines);
}