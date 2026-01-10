using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models;

public sealed record FileComplexityMetrics(
    IReadOnlyDictionary<string, int> Cognitive,
    IReadOnlyDictionary<string, int> Cyclomatic,
    IReadOnlyDictionary<string, int> Indentation,
    IReadOnlyDictionary<string, int> TypesByDocId,
    IReadOnlyDictionary<string, int> MethodsByDocId)
{
    public IReadOnlyDictionary<string, int> GetValues(ComplexityMeasureType measureType)
    {
        return measureType switch
        {
            ComplexityMeasureType.Cognitive => Cognitive,
            ComplexityMeasureType.Cyclomatic => Cyclomatic,
            ComplexityMeasureType.Indentation => Indentation,
            _ => Cognitive
        };
    }
}
