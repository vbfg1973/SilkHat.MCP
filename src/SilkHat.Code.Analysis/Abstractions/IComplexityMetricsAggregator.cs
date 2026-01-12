using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface IComplexityMetricsAggregator
    {
        Task<FileComplexityMetrics> GetMetricsAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CancellationToken cancellationToken);

        void Invalidate(Guid configId);
    }
}