namespace SilkHat.Code.Core.Dtos;

public sealed record SymbolOutlineNodeDto(
    string SymbolKey,
    string Name,
    string SymbolKind,
    string RealType,
    IReadOnlyList<SymbolOutlineNodeDto> Children);
