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
    private readonly ConcurrentBag<GraphEdgeDto> _edges = new();

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

        var edge = new TaggedEdge<Guid, EdgeType>(sourceId, targetId, type);
        if (_graph.AddEdge(edge))
        {
            _edges.Add(new GraphEdgeDto(sourceId, targetId, type, attributes));
            return true;
        }

        return false;
    }

    public IEnumerable<GraphNodeDto> Nodes => _nodes.Values;

    public IEnumerable<GraphEdgeDto> Edges => _edges;

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

    public IEnumerable<GraphEdgeDto> GetOutEdges(Guid nodeId, EdgeType? type = null)
    {
        if (!_graph.TryGetOutEdges(nodeId, out var edges))
        {
            return Array.Empty<GraphEdgeDto>();
        }

        var filtered = edges.Where(e => !type.HasValue || e.Tag == type.Value)
            .Select(e => _edges.First(ed => ed.SourceId == e.Source && ed.TargetId == e.Target && ed.EdgeType == e.Tag))
            .ToList();

        return filtered;
    }

    public IEnumerable<GraphEdgeDto> GetInEdges(Guid nodeId, EdgeType? type = null)
    {
        var filtered = _graph.Edges
            .Where(e => e.Target == nodeId)
            .Where(e => !type.HasValue || e.Tag == type.Value)
            .Select(e => _edges.First(ed => ed.SourceId == e.Source && ed.TargetId == e.Target && ed.EdgeType == e.Tag))
            .ToList();

        return filtered;
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
        return new GraphSnapshot(_nodes.Values.ToList(), _edges.ToList());
    }
}
