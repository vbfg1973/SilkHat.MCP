using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IGraphStoreProvider
{
    /// <summary>
    /// Gets (or creates) the graph store for a given solution id.
    /// </summary>
    GraphStore GetOrAdd(string solutionId);

    /// <summary>
    /// Tries to get an existing graph store for a given solution id.
    /// </summary>
    bool TryGet(string solutionId, out GraphStore? store);

    /// <summary>
    /// Removes the graph store for a given solution id.
    /// </summary>
    void Remove(string solutionId);
}
