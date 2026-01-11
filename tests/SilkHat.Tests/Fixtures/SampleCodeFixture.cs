using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services.Complexity;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Fixtures;

/// <summary>
/// Builds a small, reusable compilation/workspace for tests that need Roslyn state.
/// Metrics are cached once per test collection.
/// </summary>
public sealed class SampleCodeFixture
{
    public SampleCodeFixture()
    {
        ConfigId = Guid.NewGuid();
        (RepositoryWorkspace, SolutionWorkspace, Aggregator) = BuildWorkspaceAsync().GetAwaiter().GetResult();
        _metrics = new Lazy<Task<FileComplexityMetrics>>(() =>
            Aggregator.GetMetricsAsync(ConfigId, RepositoryWorkspace, SolutionWorkspace, CancellationToken.None));
    }

    public Guid ConfigId { get; }
    public CodeRepositoryWorkspace RepositoryWorkspace { get; }
    public CodeSolutionWorkspace SolutionWorkspace { get; }
    public IComplexityMetricsAggregator Aggregator { get; }

    private readonly Lazy<Task<FileComplexityMetrics>> _metrics;

    public Task<FileComplexityMetrics> GetCachedMetricsAsync() => _metrics.Value;

    private static async Task<(CodeRepositoryWorkspace Repo, CodeSolutionWorkspace Solution, IComplexityMetricsAggregator Aggregator)> BuildWorkspaceAsync()
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
        var compilation = await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

        var solutionWorkspace = new CodeSolutionWorkspace(
            "solution-1",
            "Sample",
            "/repo/Sample.sln",
            "./Sample.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
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

        return (repositoryWorkspace, solutionWorkspace, aggregator);
    }
}
