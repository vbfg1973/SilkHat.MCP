namespace SilkHat.Code.Analysis.Graph;

public sealed record ParameterMetadata(
    string Name,
    string? TypeId,
    int Ordinal,
    bool IsOptional,
    IReadOnlyDictionary<string, string>? Attributes = null);
