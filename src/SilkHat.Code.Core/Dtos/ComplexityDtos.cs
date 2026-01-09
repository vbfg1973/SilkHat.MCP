namespace SilkHat.Code.Core.Dtos;

public enum ComplexityMeasureType
{
    Cognitive = 1,
    Cyclomatic = 2,
    Indentation = 3
}

public enum ComplexityTargetKind
{
    Method = 1,
    NamedType = 2
}

public sealed record ComplexityResultDto(
    string DocumentationId,
    ComplexityMeasureType MeasureType,
    ComplexityTargetKind TargetKind,
    NamedTypeKind? TargetTypeKind,
    int Value);
