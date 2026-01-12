namespace SilkHat.Code.Analysis.Graph
{
    public sealed record GraphSnapshot(
        IReadOnlyList<GraphNodeDto> Nodes,
        IReadOnlyList<GraphEdgeDto> Edges);
}