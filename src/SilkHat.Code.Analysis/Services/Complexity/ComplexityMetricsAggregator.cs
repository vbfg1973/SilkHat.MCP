using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services.Complexity;

public sealed class ComplexityMetricsAggregator : IComplexityMetricsAggregator
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private readonly IComplexityStrategyFactory _strategyFactory;
    private readonly ConcurrentDictionary<string, Lazy<Task<FileComplexityMetrics>>> _cache = new();

    public ComplexityMetricsAggregator(IComplexityStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public Task<FileComplexityMetrics> GetMetricsAsync(
        Guid configId,
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        CancellationToken cancellationToken)
    {
        if (workspace is null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        var key = BuildKey(configId, solution.SolutionId);
        var lazy = _cache.GetOrAdd(
            key,
            _ => new Lazy<Task<FileComplexityMetrics>>(
                () => BuildMetricsAsync(workspace, solution, cancellationToken)));

        return lazy.Value;
    }

    public void Invalidate(Guid configId)
    {
        var prefix = configId.ToString("N") + ":";
        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private async Task<FileComplexityMetrics> BuildMetricsAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        CancellationToken cancellationToken)
    {
        var cognitive = new ConcurrentDictionary<string, int>(PathComparer);
        var cyclomatic = new ConcurrentDictionary<string, int>(PathComparer);
        var indentation = new ConcurrentDictionary<string, int>(PathComparer);
        var types = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var methods = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var strategies = new Dictionary<ComplexityMeasureType, IComplexityStrategy>
        {
            [ComplexityMeasureType.Cognitive] = _strategyFactory.GetStrategy(ComplexityMeasureType.Cognitive),
            [ComplexityMeasureType.Cyclomatic] = _strategyFactory.GetStrategy(ComplexityMeasureType.Cyclomatic),
            [ComplexityMeasureType.Indentation] = _strategyFactory.GetStrategy(ComplexityMeasureType.Indentation)
        };

        var treeToCompilation = new Dictionary<SyntaxTree, Compilation>();
        foreach (var compilation in solution.Compilations.Values)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                if (!treeToCompilation.ContainsKey(tree))
                {
                    treeToCompilation[tree] = compilation;
                }
            }
        }

        var syntaxTrees = treeToCompilation.Keys
            .Where(tree => !string.IsNullOrWhiteSpace(tree.FilePath))
            .ToList();

        await Parallel.ForEachAsync(
            syntaxTrees,
            cancellationToken,
            async (tree, ct) =>
            {
                if (!treeToCompilation.TryGetValue(tree, out var compilation))
                {
                    return;
                }

                var semanticModel = compilation.GetSemanticModel(tree);
                var sourceText = await tree.GetTextAsync(ct);
                var relativePath = SolutionIdentity.NormalizeRelativePath(workspace.RootPath, tree.FilePath);

                var methodNodes = tree.GetRoot(ct)
                    .DescendantNodes()
                    .OfType<BaseMethodDeclarationSyntax>()
                    .ToList();

                foreach (var method in methodNodes)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(method, ct);
                    var typeDocId = methodSymbol?.ContainingType is not null
                        ? DocumentationIdUtility.GetDocumentationId(methodSymbol.ContainingType)
                        : null;
                    var methodDocId = methodSymbol is not null
                        ? DocumentationIdUtility.GetDocumentationId(methodSymbol)
                        : null;

                    foreach (var (measure, strategy) in strategies)
                    {
                        var value = strategy.Compute(method, semanticModel, sourceText);
                        AddValue(measure, relativePath, value, cognitive, cyclomatic, indentation);
                        if (!string.IsNullOrWhiteSpace(typeDocId))
                        {
                            AddByDocId(measure, typeDocId!, value, types, methods, isMethod: false);
                        }

                        if (!string.IsNullOrWhiteSpace(methodDocId))
                        {
                            AddByDocId(measure, methodDocId!, value, types, methods, isMethod: true);
                        }
                    }
                }
            });

        return new FileComplexityMetrics(cognitive, cyclomatic, indentation, types, methods);
    }

    private static void AddValue(
        ComplexityMeasureType measure,
        string path,
        int value,
        ConcurrentDictionary<string, int> cognitive,
        ConcurrentDictionary<string, int> cyclomatic,
        ConcurrentDictionary<string, int> indentation)
    {
        if (value == 0)
        {
            return;
        }

        var target = measure switch
        {
            ComplexityMeasureType.Cognitive => cognitive,
            ComplexityMeasureType.Cyclomatic => cyclomatic,
            ComplexityMeasureType.Indentation => indentation,
            _ => cognitive
        };

        target.AddOrUpdate(path, value, (_, existing) => existing + value);
    }

    private static void AddByDocId(
        ComplexityMeasureType measure,
        string docId,
        int value,
        ConcurrentDictionary<string, int> types,
        ConcurrentDictionary<string, int> methods,
        bool isMethod)
    {
        if (value == 0 || string.IsNullOrWhiteSpace(docId))
        {
            return;
        }

        var target = isMethod ? methods : types;
        target.AddOrUpdate(docId, value, (_, existing) => existing + value);
    }

    private static string BuildKey(Guid configId, string solutionId)
    {
        return $"{configId:N}:{solutionId}";
    }
}
