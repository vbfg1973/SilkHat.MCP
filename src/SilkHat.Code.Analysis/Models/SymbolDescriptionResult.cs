namespace SilkHat.Code.Analysis.Models;

public enum SymbolDescriptionStatus
{
    Success,
    NotFound,
    Unsupported
}

public sealed record SymbolDescriptionResult<T>(
    SymbolDescriptionStatus Status,
    T? Description,
    string? Error);
