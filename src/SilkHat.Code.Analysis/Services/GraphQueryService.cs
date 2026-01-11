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

    public bool TryGetNodeByKey(string solutionId, string key, out GraphNodeDto? node)
    {
        node = null;
        if (string.IsNullOrWhiteSpace(solutionId) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_storeProvider.TryGet(solutionId, out var store) && store is not null)
        {
            return store.TryGetNodeByKey(key, out node) && node is not null;
        }

        return false;
    }

    public IReadOnlyList<GraphNodeDto> GetOutgoingNodes(string solutionId, string key, EdgeType? edgeType = null)
    {
        if (!_storeProvider.TryGet(solutionId, out var store) || store is null)
        {
            return Array.Empty<GraphNodeDto>();
        }

        if (!store.TryGetNodeByKey(key, out var node) || node is null)
        {
            return Array.Empty<GraphNodeDto>();
        }

        return store.GetOutEdges(node.Id, edgeType)
            .Select(edge =>
            {
                store.TryGetNode(edge.TargetId, out var target);
                return target;
            })
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
    }

    public IReadOnlyList<GraphNodeDto> GetIncomingNodes(string solutionId, string key, EdgeType? edgeType = null)
    {
        if (!_storeProvider.TryGet(solutionId, out var store) || store is null)
        {
            return Array.Empty<GraphNodeDto>();
        }

        if (!store.TryGetNodeByKey(key, out var node) || node is null)
        {
            return Array.Empty<GraphNodeDto>();
        }

        return store.GetInEdges(node.Id, edgeType)
            .Select(edge =>
            {
                store.TryGetNode(edge.SourceId, out var source);
                return source;
            })
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
    }
}
