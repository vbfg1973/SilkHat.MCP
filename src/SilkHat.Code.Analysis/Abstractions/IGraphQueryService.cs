using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IGraphQueryService
{
    /// <summary>
    /// Returns a snapshot of the graph for the specified solution, or an empty snapshot if none exists.
    /// </summary>
    GraphSnapshot GetSnapshot(string solutionId);
}
