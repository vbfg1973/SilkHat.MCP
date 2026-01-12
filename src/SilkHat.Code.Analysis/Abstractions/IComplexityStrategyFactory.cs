using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface IComplexityStrategyFactory
    {
        IComplexityStrategy GetStrategy(ComplexityMeasureType measureType);
    }
}