using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Code.Analysis.Services;

public sealed class GraphQueryService : IGraphQueryService
{
    private readonly IGraphStoreProvider _storeProvider;

    public GraphQueryService(IGraphStoreProvider storeProvider)
    {
        _storeProvider = storeProvider;
    }

    public GraphSnapshot GetSnapshot(string solutionId)
    {
        if (_storeProvider.TryGet(solutionId, out var store) && store is not null)
        {
            return store.ToSnapshot();
        }

        return new GraphSnapshot(Array.Empty<GraphNodeDto>(), Array.Empty<GraphEdgeDto>());
    }
}
