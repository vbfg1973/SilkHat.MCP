namespace SilkHat.Ui.Models;

public sealed record CodeSymbolOutlineNodeModel(
    string SymbolKey,
    string Name,
    string SymbolKind,
    string RealType,
    IReadOnlyList<CodeSymbolOutlineNodeModel> Children);
