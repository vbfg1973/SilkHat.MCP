using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models
{
    public sealed record FileComplexityMetrics(
        IReadOnlyDictionary<string, int> Cognitive,
        IReadOnlyDictionary<string, int> Cyclomatic,
        IReadOnlyDictionary<string, int> Indentation,
        IReadOnlyDictionary<ComplexityMeasureType, IReadOnlyDictionary<string, int>> TypesByMeasure,
        IReadOnlyDictionary<ComplexityMeasureType, IReadOnlyDictionary<string, int>> MethodsByMeasure)
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

        public IReadOnlyDictionary<string, int> GetTypes(ComplexityMeasureType measureType)
        {
            return TypesByMeasure.TryGetValue(measureType, out var values)
                ? values
                : TypesByMeasure.GetValueOrDefault(ComplexityMeasureType.Cognitive, new Dictionary<string, int>());
        }

        public IReadOnlyDictionary<string, int> GetMethods(ComplexityMeasureType measureType)
        {
            return MethodsByMeasure.TryGetValue(measureType, out var values)
                ? values
                : MethodsByMeasure.GetValueOrDefault(ComplexityMeasureType.Cognitive, new Dictionary<string, int>());
        }
    }
}