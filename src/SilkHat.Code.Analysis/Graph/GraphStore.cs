using System.Collections.Concurrent;
using QuikGraph;

namespace SilkHat.Code.Analysis.Graph;

/// <summary>
/// In-memory graph store backed by QuikGraph, with typed edges and serializable snapshots.
/// </summary>
public sealed class GraphStore
{
    private readonly AdjacencyGraph<Guid, TaggedEdge<Guid, EdgeType>> _graph = new();
    private readonly ConcurrentDictionary<Guid, GraphNodeDto> _nodes = new();
    private readonly ConcurrentDictionary<string, Guid> _nodeIdsByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<GraphNodeKind, ConcurrentDictionary<string, Guid>> _nodeIdsByTypeAndKey = new();
    private readonly ConcurrentDictionary<(Guid SourceId, Guid TargetId, EdgeType Type), GraphEdgeDto> _edges = new();

    public bool AddNode(GraphNodeDto node)
    {
        if (node is null || node.IsEmpty)
        {
            return false;
        }

        if (_nodes.TryAdd(node.Id, node))
        {
            _graph.AddVertex(node.Id);
            _nodeIdsByKey[node.Key] = node.Id;
            var typed = _nodeIdsByTypeAndKey.GetOrAdd(node.Kind, _ => new ConcurrentDictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));
            typed[node.Key] = node.Id;
            return true;
        }

        return false;
    }

    public bool AddEdge(Guid sourceId, Guid targetId, EdgeType type, IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (!_nodes.ContainsKey(sourceId) || !_nodes.ContainsKey(targetId))
        {
            return false;
        }

        var key = (sourceId, targetId, type);
        if (_edges.ContainsKey(key))
        {
            return false;
        }

        var edge = new TaggedEdge<Guid, EdgeType>(sourceId, targetId, type);
        if (!_graph.AddEdge(edge))
        {
            return false;
        }

        if (_edges.TryAdd(key, new GraphEdgeDto(sourceId, targetId, type, attributes)))
        {
            return true;
        }

        _graph.RemoveEdge(edge);
        return false;
    }

    public IEnumerable<GraphNodeDto> Nodes => _nodes.Values;

    public IEnumerable<GraphEdgeDto> Edges => _edges.Values;

    public bool TryGetNode(Guid id, out GraphNodeDto? node)
    {
        if (_nodes.TryGetValue(id, out var value))
        {
            node = value;
            return true;
        }

        node = null;
        return false;
    }

    public bool TryGetNodeByKey(string key, out GraphNodeDto? node)
    {
        node = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_nodeIdsByKey.TryGetValue(key, out var id) && _nodes.TryGetValue(id, out var found))
        {
            node = found;
            return true;
        }

        return false;
    }

    public bool TryGetNodeByTypeAndKey(GraphNodeKind type, string key, out GraphNodeDto? node)
    {
        node = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_nodeIdsByTypeAndKey.TryGetValue(type, out var byKey)
            && byKey.TryGetValue(key, out var id)
            && _nodes.TryGetValue(id, out var found))
        {
            node = found;
            return true;
        }

        return false;
    }

    public IEnumerable<GraphEdgeDto> GetOutEdges(Guid nodeId, EdgeType? type = null)
    {
        return _edges.Values.Where(e =>
            e.SourceId == nodeId &&
            (!type.HasValue || e.EdgeType == type.Value));
    }

    public IEnumerable<GraphEdgeDto> GetInEdges(Guid nodeId, EdgeType? type = null)
    {
        return _edges.Values.Where(e =>
            e.TargetId == nodeId &&
            (!type.HasValue || e.EdgeType == type.Value));
    }

    public int GetInDegree(Guid nodeId, EdgeType? type = null)
    {
        return GetInEdges(nodeId, type).Count();
    }

    public int GetOutDegree(Guid nodeId, EdgeType? type = null)
    {
        return GetOutEdges(nodeId, type).Count();
    }

    public GraphSnapshot ToSnapshot()
    {
        return new GraphSnapshot(_nodes.Values.ToList(), _edges.Values.ToList());
    }
}
