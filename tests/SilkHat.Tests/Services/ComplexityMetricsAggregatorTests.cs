using SilkHat.Code.Core.Dtos;
using SilkHat.Tests.Fixtures;

namespace SilkHat.Tests.Services
{
    [CollectionDefinition("SampleCodeCollection")]
    public sealed class SampleCodeCollection : ICollectionFixture<SampleCodeFixture>
    {
    }

    [Collection("SampleCodeCollection")]
    public sealed class ComplexityMetricsAggregatorTests
    {
        private readonly SampleCodeFixture _fixture;

        public ComplexityMetricsAggregatorTests(SampleCodeFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetMetricsAsync_ComputesFileComplexity()
        {
            var metrics = await _fixture.GetCachedMetricsAsync();

            Assert.True(metrics.Cognitive["./Sample.cs"] > 0);
            Assert.True(metrics.Cyclomatic["./Sample.cs"] > 0);
            Assert.True(metrics.Indentation["./Sample.cs"] > 0);
            Assert.NotEmpty(metrics.GetMethods(ComplexityMeasureType.Cognitive));
        }

        [Fact]
        public async Task GetMetricsAsync_DoesNotMixMeasuresAcrossBuckets()
        {
            var metrics = await _fixture.GetCachedMetricsAsync();

            // For the type, cognitive/cyclomatic/indentation buckets should all be present and independent
            var typeDocId = metrics.GetTypes(ComplexityMeasureType.Cognitive).Keys.First();

            var cognitiveForType = metrics.GetTypes(ComplexityMeasureType.Cognitive)[typeDocId];
            var cyclomaticForType = metrics.GetTypes(ComplexityMeasureType.Cyclomatic)[typeDocId];
            var indentationForType = metrics.GetTypes(ComplexityMeasureType.Indentation)[typeDocId];

            Assert.True(cognitiveForType >= 0);
            Assert.True(cyclomaticForType >= 0);
            Assert.True(indentationForType >= 0);

            Assert.NotEqual(cognitiveForType, cyclomaticForType); // branchy method should bump cyclomatic differently
        }
    }
}