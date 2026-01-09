using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class MethodComplexityService : IMethodComplexityService
{
    private readonly IComplexityStrategyFactory _strategyFactory;

    public MethodComplexityService(IComplexityStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public Task<ComplexityResultDto?> GetMethodComplexityAsync(
        CodeSolutionWorkspace solution,
        string docId,
        ComplexityMeasureType measure,
        CancellationToken cancellationToken)
    {
        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        if (string.IsNullOrWhiteSpace(docId))
        {
            return Task.FromResult<ComplexityResultDto?>(null);
        }

        var method = DocumentationIdUtility.FindMethodByDocumentationId(solution, docId);
        if (method is null)
        {
            return Task.FromResult<ComplexityResultDto?>(null);
        }

        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef is null)
        {
            return Task.FromResult<ComplexityResultDto?>(null);
        }

        var syntaxNode = syntaxRef.GetSyntax(cancellationToken);
        if (syntaxNode is not BaseMethodDeclarationSyntax methodSyntax)
        {
            return Task.FromResult<ComplexityResultDto?>(null);
        }

        var compilation = GetCompilationForSymbol(solution, method);
        if (compilation is null)
        {
            return Task.FromResult<ComplexityResultDto?>(null);
        }

        var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree, ignoreAccessibility: true);
        var strategy = _strategyFactory.GetStrategy(measure);
        var value = strategy.Compute(methodSyntax, semanticModel, methodSyntax.SyntaxTree.GetText());

        return Task.FromResult<ComplexityResultDto?>(new ComplexityResultDto(
            docId,
            measure,
            ComplexityTargetKind.Method,
            null,
            value));
    }

    private static Compilation? GetCompilationForSymbol(CodeSolutionWorkspace solution, IMethodSymbol method)
    {
        var assemblyName = method.ContainingAssembly?.Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return null;
        }

        return solution.Compilations.Values.FirstOrDefault(compilation =>
            string.Equals(compilation.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase));
    }
}
