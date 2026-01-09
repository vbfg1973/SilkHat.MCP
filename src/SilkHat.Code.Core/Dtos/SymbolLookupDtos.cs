namespace SilkHat.Code.Core.Dtos;

public sealed record SymbolLookupRequest(
    string? DocumentationId,
    string SymbolKey,
    string ExpectedKind);

public sealed record SymbolLookupResultDto(
    bool Found,
    string Kind,
    string Name,
    string? Namespace,
    string? AssemblyName,
    string? DocumentationId);
