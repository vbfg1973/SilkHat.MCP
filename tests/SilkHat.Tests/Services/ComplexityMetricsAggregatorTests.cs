using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services.Complexity;

namespace SilkHat.Tests.Services;

using SilkHat.Tests.Fixtures;

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
        Assert.NotEmpty(metrics.GetMethods(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Cognitive));
    }

    [Fact]
    public async Task GetMetricsAsync_DoesNotMixMeasuresAcrossBuckets()
    {
        var metrics = await _fixture.GetCachedMetricsAsync();

        // For the type, cognitive/cyclomatic/indentation buckets should all be present and independent
        var typeDocId = metrics.GetTypes(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Cognitive).Keys.First();

        var cognitiveForType = metrics.GetTypes(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Cognitive)[typeDocId];
        var cyclomaticForType = metrics.GetTypes(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Cyclomatic)[typeDocId];
        var indentationForType = metrics.GetTypes(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Indentation)[typeDocId];

        Assert.True(cognitiveForType >= 0);
        Assert.True(cyclomaticForType >= 0);
        Assert.True(indentationForType >= 0);

        Assert.NotEqual(cognitiveForType, cyclomaticForType); // branchy method should bump cyclomatic differently
    }
}
