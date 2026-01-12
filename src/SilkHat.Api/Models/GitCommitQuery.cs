namespace SilkHat.Api.Models
{
    public sealed class GitCommitQuery
    {
        public string? Author { get; init; }
        public string? Sha { get; init; }
        public DateTimeOffset? Since { get; init; }
        public DateTimeOffset? Until { get; init; }
        public bool? Merge { get; init; }
        public string? Path { get; init; }
        public int? PageNumber { get; init; }
        public int? PageSize { get; init; }
    }
}