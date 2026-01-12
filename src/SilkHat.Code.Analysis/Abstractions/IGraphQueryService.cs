using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface IGraphQueryService
    {
        /// <summary>
        ///     Returns a snapshot of the graph for the specified solution, or an empty snapshot if none exists.
        /// </summary>
        GraphSnapshot GetSnapshot(string solutionId);

        /// <summary>
        ///     Attempts to get a node by its key (often a DocumentationId or SymbolKey).
        /// </summary>
        bool TryGetNodeByKey(string solutionId, string key, out GraphNodeDto? node);

        /// <summary>
        ///     Returns outgoing neighbor nodes for the specified node key filtered by edge type (optional).
        /// </summary>
        IReadOnlyList<GraphNodeDto> GetOutgoingNodes(string solutionId, string key, EdgeType? edgeType = null);

        /// <summary>
        ///     Returns incoming neighbor nodes for the specified node key filtered by edge type (optional).
        /// </summary>
        IReadOnlyList<GraphNodeDto> GetIncomingNodes(string solutionId, string key, EdgeType? edgeType = null);
    }
}