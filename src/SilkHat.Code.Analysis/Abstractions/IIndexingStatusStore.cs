using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IIndexingStatusStore
{
    void InitializeSolution(string solutionId);
    void SetJobRunning(string solutionId, IndexJobType jobType, string? message = null);
    void SetJobCompleted(string solutionId, IndexJobType jobType, string? message = null);
    void SetJobFailed(string solutionId, IndexJobType jobType, string? message = null);
    IReadOnlyCollection<IndexJobStatus> GetStatus(string solutionId);
}
