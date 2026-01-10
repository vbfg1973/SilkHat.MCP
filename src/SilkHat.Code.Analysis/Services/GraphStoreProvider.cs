using System.Collections.Concurrent;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Code.Analysis.Services;

public sealed class GraphStoreProvider : IGraphStoreProvider
{
    private readonly ConcurrentDictionary<string, GraphStore> _stores = new(StringComparer.OrdinalIgnoreCase);

    public GraphStore GetOrAdd(string solutionId)
    {
        if (string.IsNullOrWhiteSpace(solutionId))
        {
            throw new ArgumentNullException(nameof(solutionId));
        }

        return _stores.GetOrAdd(solutionId, _ => new GraphStore());
    }

    public bool TryGet(string solutionId, out GraphStore? store)
    {
        if (string.IsNullOrWhiteSpace(solutionId))
        {
            store = null;
            return false;
        }

        return _stores.TryGetValue(solutionId, out store);
    }

    public void Remove(string solutionId)
    {
        if (string.IsNullOrWhiteSpace(solutionId))
        {
            return;
        }

        _stores.TryRemove(solutionId, out _);
    }
}
