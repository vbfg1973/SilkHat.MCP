using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models;

public enum CodeFileSymbolsStatus
{
    Success,
    NotFound,
    InvalidPath
}

public sealed record CodeFileSymbolsResult(
    CodeFileSymbolsStatus Status,
    IReadOnlyList<SymbolOutlineNodeDto>? Symbols,
    string? Message);
