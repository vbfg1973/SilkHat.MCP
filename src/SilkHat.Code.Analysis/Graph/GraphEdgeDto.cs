namespace SilkHat.Code.Analysis.Graph;

public sealed record GraphEdgeDto(
    Guid SourceId,
    Guid TargetId,
    EdgeType EdgeType,
    IReadOnlyDictionary<string, string>? Attributes = null);
