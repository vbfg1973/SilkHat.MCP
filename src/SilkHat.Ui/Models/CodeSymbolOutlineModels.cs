namespace SilkHat.Ui.Models;

public sealed record CodeSymbolOutlineNodeModel(
    string SymbolKey,
    string? DocumentationId,
    string Name,
    string SymbolKind,
    string RealType,
    IReadOnlyList<CodeSymbolOutlineNodeModel> Children);
