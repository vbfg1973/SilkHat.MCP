namespace SilkHat.Git.Core.Dtos
{
    public enum GitTreeEntryType
    {
        File = 0,
        Directory = 1
    }

    public sealed record GitPathChangeDto(
        string CommitSha,
        string Author,
        DateTimeOffset CommitDateUtc);

    public sealed record GitTreeEntryDto(
        string Path,
        string Name,
        GitTreeEntryType Type,
        GitPathChangeDto? LastChange);
}