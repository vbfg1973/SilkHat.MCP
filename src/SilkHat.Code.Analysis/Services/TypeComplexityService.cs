using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class TypeComplexityService : ITypeComplexityService
{
    private readonly IComplexityStrategyFactory _strategyFactory;

    public TypeComplexityService(IComplexityStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public Task<TypeComplexityResult> GetTypeComplexityAsync(
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
            return Task.FromResult(new TypeComplexityResult(TypeComplexityStatus.NotFound, null));
        }

        var typeSymbol = DocumentationIdUtility.FindTypeByDocumentationId(solution, docId);
        if (typeSymbol is null)
        {
            return Task.FromResult(new TypeComplexityResult(TypeComplexityStatus.NotFound, null));
        }

        if (typeSymbol.TypeKind == TypeKind.Interface)
        {
            return Task.FromResult(new TypeComplexityResult(TypeComplexityStatus.InterfaceNotSupported, null));
        }

        var total = 0;
        var strategy = _strategyFactory.GetStrategy(measure);

        foreach (var method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (!IsSupportedMethod(method))
            {
                continue;
            }

            var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef is null)
            {
                continue;
            }

            var syntaxNode = syntaxRef.GetSyntax(cancellationToken);
            if (syntaxNode is not BaseMethodDeclarationSyntax methodSyntax)
            {
                continue;
            }

            var compilation = GetCompilationForSymbol(solution, method);
            if (compilation is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree, ignoreAccessibility: true);
            var value = strategy.Compute(methodSyntax, semanticModel, methodSyntax.SyntaxTree.GetText());
            total += value;
        }

        var targetKind = MapNamedTypeKind(typeSymbol);

        var result = new ComplexityResultDto(
            docId,
            measure,
            ComplexityTargetKind.NamedType,
            targetKind,
            total);

        return Task.FromResult(new TypeComplexityResult(TypeComplexityStatus.Success, result));
    }

    private static bool IsSupportedMethod(IMethodSymbol method)
    {
        return method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor or MethodKind.StaticConstructor;
    }

    private static NamedTypeKind MapNamedTypeKind(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.IsRecord)
        {
            return typeSymbol.TypeKind == TypeKind.Struct ? NamedTypeKind.Struct : NamedTypeKind.Record;
        }

        return typeSymbol.TypeKind switch
        {
            TypeKind.Class => NamedTypeKind.Class,
            TypeKind.Struct => NamedTypeKind.Struct,
            TypeKind.Interface => NamedTypeKind.Interface,
            TypeKind.Enum => NamedTypeKind.Enum,
            TypeKind.Delegate => NamedTypeKind.Delegate,
            _ => NamedTypeKind.Unknown
        };
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
