using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services.Complexity
{
    public sealed class ComplexityStrategyFactory : IComplexityStrategyFactory
    {
        private readonly IReadOnlyDictionary<ComplexityMeasureType, IComplexityStrategy> _strategies;

        public ComplexityStrategyFactory(IEnumerable<IComplexityStrategy> strategies)
        {
            _strategies = strategies.ToDictionary(strategy => strategy.MeasureType);
        }

        public IComplexityStrategy GetStrategy(ComplexityMeasureType measureType)
        {
            if (_strategies.TryGetValue(measureType, out var strategy)) return strategy;

            throw new InvalidOperationException($"No complexity strategy registered for {measureType}.");
        }
    }
}