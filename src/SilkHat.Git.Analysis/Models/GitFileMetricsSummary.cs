namespace SilkHat.Git.Analysis.Models
{
    public sealed record GitFileMetricsSummary(
        IReadOnlyDictionary<string, int> ChangeCounts,
        IReadOnlyDictionary<string, int> AuthorCounts,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> AuthorsByFile);
}