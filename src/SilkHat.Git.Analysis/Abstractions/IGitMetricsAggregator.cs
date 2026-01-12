using SilkHat.Git.Analysis.Models;

namespace SilkHat.Git.Analysis.Abstractions
{
    public interface IGitMetricsAggregator
    {
        Task<GitFileMetricsSummary> GetMetricsAsync(
            Guid configId,
            string repoRoot,
            CancellationToken cancellationToken);

        void Invalidate(Guid configId);
    }
}