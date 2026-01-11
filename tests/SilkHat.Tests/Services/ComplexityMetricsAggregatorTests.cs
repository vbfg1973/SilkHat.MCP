using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services.Complexity;

namespace SilkHat.Tests.Services;

public sealed class ComplexityMetricsAggregatorTests
{
    [Fact]
    public async Task GetMetricsAsync_ComputesFileComplexity()
    {
        var rootPath = "/repo";
        var filePath = "/repo/Sample.cs";
        var source = """
            namespace Sample;
            public class Foo
            {
                public void DoWork()
                {
                    if (true)
                    {
                    }
                }
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Sample", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        project = project.AddDocument("Sample.cs", SourceText.From(source), filePath: filePath).Project;
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);

        var solutionWorkspace = new CodeSolutionWorkspace(
            "solution-1",
            "Sample",
            "/repo/Sample.sln",
            "./Sample.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, Compilation> { ["sample"] = compilation! });

        var repositoryWorkspace = new CodeRepositoryWorkspace(rootPath, new Dictionary<string, CodeSolutionWorkspace>
        {
            [solutionWorkspace.SolutionId] = solutionWorkspace
        });

        var strategies = new IComplexityStrategy[]
        {
            new CognitiveComplexityStrategy(),
            new CyclomaticComplexityStrategy(),
            new IndentationComplexityStrategy()
        };
        var factory = new ComplexityStrategyFactory(strategies);
        var aggregator = new ComplexityMetricsAggregator(factory);

        var metrics = await aggregator.GetMetricsAsync(Guid.NewGuid(), repositoryWorkspace, solutionWorkspace, CancellationToken.None);

        Assert.True(metrics.Cognitive["./Sample.cs"] > 0);
        Assert.True(metrics.Cyclomatic["./Sample.cs"] > 0);
        Assert.True(metrics.Indentation["./Sample.cs"] > 0);
        Assert.NotEmpty(metrics.GetMethods(SilkHat.Code.Core.Dtos.ComplexityMeasureType.Cognitive));
    }

    [Fact]
    public async Task GetMetricsAsync_DoesNotMixMeasuresAcrossBuckets()
    {
        var source = """
            namespace Sample;
            public class Foo
            {
                public void Simple() { }
                public void Branchy(bool x)
                {
                    if (x)
                    {
                        return;
                    }
                }
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Sample", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        project = project.AddDocument("Sample.cs", SourceText.From(source), filePath: "/repo/Sample.cs").Project;
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);

        var solutionWorkspace = new CodeSolutionWorkspace(
            "solution-1",
            "Sample",
            "/repo/Sample.sln",
            "./Sample.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<SilkHat.Code.Core.Dtos.CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, SilkHat.Code.Core.Dtos.NamedTypeDto>(),
            new Dictionary<string, Compilation> { ["sample"] = compilation! });

        var repositoryWorkspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solutionWorkspace.SolutionId] = solutionWorkspace
        });

        var strategies = new IComplexityStrategy[]
        {
            new CognitiveComplexityStrategy(),
            new CyclomaticComplexityStrategy(),
            new IndentationComplexityStrategy()
        };
        var factory = new ComplexityStrategyFactory(strategies);
        var aggregator = new ComplexityMetricsAggregator(factory);

        var metrics = await aggregator.GetMetricsAsync(Guid.NewGuid(), repositoryWorkspace, solutionWorkspace, CancellationToken.None);

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
