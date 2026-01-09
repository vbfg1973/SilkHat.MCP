using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Analysis.Services.Complexity;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class TypeComplexityServiceTests
{
    [Theory]
    [InlineData(ComplexityMeasureType.Cyclomatic, 10)]
    [InlineData(ComplexityMeasureType.Cognitive, 9)]
    [InlineData(ComplexityMeasureType.Indentation, 256)]
    public async Task GetTypeComplexity_ReturnsExpectedResult(ComplexityMeasureType measure, int expected)
    {
        var (solution, docId) = await LoadSolutionAndTypeDocIdAsync();
        var factory = BuildFactory();
        var service = new TypeComplexityService(factory);

        var result = await service.GetTypeComplexityAsync(
            solution,
            docId,
            measure,
            CancellationToken.None);

        Assert.Equal(TypeComplexityStatus.Success, result.Status);
        Assert.NotNull(result.Result);
        Assert.Equal(docId, result.Result!.DocumentationId);
        Assert.Equal(ComplexityTargetKind.NamedType, result.Result.TargetKind);
        Assert.Equal(NamedTypeKind.Class, result.Result.TargetTypeKind);
        Assert.Equal(expected, result.Result.Value);
    }

    [Fact]
    public async Task GetTypeComplexity_ReportsInterfaceNotSupported()
    {
        var (solution, docId) = await LoadSolutionAndInterfaceDocIdAsync();
        var factory = BuildFactory();
        var service = new TypeComplexityService(factory);

        var result = await service.GetTypeComplexityAsync(
            solution,
            docId,
            ComplexityMeasureType.Cyclomatic,
            CancellationToken.None);

        Assert.Equal(TypeComplexityStatus.InterfaceNotSupported, result.Status);
        Assert.Null(result.Result);
    }

    private static IComplexityStrategyFactory BuildFactory()
    {
        var strategies = new IComplexityStrategy[]
        {
            new CognitiveComplexityStrategy(),
            new CyclomaticComplexityStrategy(),
            new IndentationComplexityStrategy()
        };
        return new ComplexityStrategyFactory(strategies);
    }

    private static async Task<(CodeSolutionWorkspace Solution, string DocId)> LoadSolutionAndTypeDocIdAsync()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
            CancellationToken.None);

        var solution = Assert.Single(workspace.Solutions.Values);
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName("SilkHat.Sample.App.ComplexitySamples");
            if (type is null)
            {
                continue;
            }

            var docId = type.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return (solution, docId);
            }
        }

        throw new InvalidOperationException("ComplexitySamples docId not found.");
    }

    private static async Task<(CodeSolutionWorkspace Solution, string DocId)> LoadSolutionAndInterfaceDocIdAsync()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
            CancellationToken.None);

        var solution = Assert.Single(workspace.Solutions.Values);
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName("SilkHat.Sample.Lib.IClock");
            if (type is null)
            {
                continue;
            }

            var docId = type.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return (solution, docId);
            }
        }

        throw new InvalidOperationException("IClock docId not found.");
    }

    private static string LocateSampleRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", "solution01", "SilkHat.Sample.sln");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate samples/solution01/SilkHat.Sample.sln.");
    }
}
