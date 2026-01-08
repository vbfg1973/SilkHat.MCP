using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services;

public sealed class MethodCallStackService : IMethodCallStackService
{
    private readonly IMethodImplementationDecisionService _decisionService;

    public MethodCallStackService(IMethodImplementationDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    public async Task<MethodCallStackResult> BuildCallStackAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        string methodSymbolKey,
        int? maxDepth,
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

        if (string.IsNullOrWhiteSpace(methodSymbolKey))
        {
            return new MethodCallStackResult(Array.Empty<MethodCallStackNode>(), true, "SymbolKey is required.");
        }

        if (!TryResolveMethodSymbol(solution, methodSymbolKey, out var rootMethod, out var rootCompilation))
        {
            return new MethodCallStackResult(Array.Empty<MethodCallStackNode>(), true, "Method symbol not found.");
        }

        var nodes = new List<MethodCallStackNode>();
        var path = new HashSet<string>(StringComparer.Ordinal);
        var rootKey = SymbolKeyUtility.GetSymbolKeyString(rootMethod, rootCompilation);
        path.Add(rootKey);

        await TraverseMethodAsync(
            workspace,
            solution,
            repositoryConfigId,
            rootMethod,
            rootCompilation,
            null,
            0,
            nodes,
            path,
            maxDepth,
            cancellationToken);

        return new MethodCallStackResult(nodes, false, null);
    }

    private async Task TraverseMethodAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        IMethodSymbol method,
        Compilation compilation,
        string? parentNodeId,
        int depth,
        List<MethodCallStackNode> nodes,
        HashSet<string> path,
        int? maxDepth,
        CancellationToken cancellationToken)
    {
        if (maxDepth.HasValue && depth > maxDepth.Value)
        {
            return;
        }

        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef is null)
        {
            return;
        }

        var syntaxNode = syntaxRef.GetSyntax(cancellationToken);
        var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree, ignoreAccessibility: true);
        var callNodes = GetCallNodes(syntaxNode)
            .OrderBy(node => node.SpanStart)
            .ToList();

        var callerInfo = MethodDescriptor.FromSymbol(method);
        var callIndex = 0;

        foreach (var callNode in callNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetMethod = ResolveCallTarget(semanticModel, callNode);
            if (targetMethod is null)
            {
                continue;
            }

            var isInterfaceTarget = targetMethod.ContainingType.TypeKind == TypeKind.Interface;
            MethodImplementationResolution? resolution = null;
            var resolvedMethod = targetMethod;

            if (isInterfaceTarget)
            {
                resolution = await _decisionService.ResolveAsync(
                    workspace,
                    solution,
                    repositoryConfigId,
                    solution.SolutionId,
                    targetMethod,
                    cancellationToken);

                if (resolution.Implementation is not null)
                {
                    resolvedMethod = resolution.Implementation;
                }
            }

            var targetInfo = MethodDescriptor.FromSymbol(resolvedMethod);
            var interfaceTypeName = isInterfaceTarget ? targetMethod.ContainingType.ToDisplayString() : null;
            var interfaceTypeDocId = isInterfaceTarget
                ? DocumentationIdUtility.GetDocumentationId(targetMethod.ContainingType)
                : null;
            var interfaceMethodDocId = isInterfaceTarget
                ? DocumentationIdUtility.GetDocumentationId(targetMethod)
                : null;
            var resolvedTypeName = isInterfaceTarget && resolvedMethod.ContainingType is not null
                ? resolvedMethod.ContainingType.ToDisplayString()
                : null;
            var resolvedTypeDocId = DocumentationIdUtility.GetDocumentationId(resolvedMethod.ContainingType);
            var resolvedMethodDocId = DocumentationIdUtility.GetDocumentationId(resolvedMethod);

            var fullyQualifiedName = BuildFullyQualifiedMethodName(targetInfo);
            var nodeId = $"{depth}_{fullyQualifiedName}_{callIndex}";
            var callSite = BuildCallSite(workspace.RootPath, callNode);

            nodes.Add(new MethodCallStackNode(
                nodeId,
                depth,
                callIndex,
                callerInfo.Namespace,
                callerInfo.TypeName,
                callerInfo.MethodName,
                callerInfo.ParameterTypes,
                BuildFullyQualifiedMethodName(callerInfo),
                targetInfo.Namespace,
                targetInfo.TypeName,
                targetInfo.MethodName,
                targetInfo.ParameterTypes,
                fullyQualifiedName,
                parentNodeId,
                callSite,
                resolution?.Decision,
                isInterfaceTarget,
                interfaceTypeName,
                interfaceTypeDocId,
                interfaceMethodDocId,
                resolvedTypeName,
                resolvedTypeDocId,
                resolvedMethodDocId,
                resolution?.DecisionRequired ?? false,
                resolution?.CandidateTypeNames ?? Array.Empty<string>(),
                resolution?.CandidateMethodDocumentationIds ?? Array.Empty<string?>()));

            callIndex++;

            if ((resolution?.DecisionRequired ?? false) && resolution?.Implementation is null)
            {
                continue;
            }

            var nextCompilation = GetCompilationForSymbol(solution, resolvedMethod) ?? compilation;
            var nextKey = SymbolKeyUtility.GetSymbolKeyString(resolvedMethod, nextCompilation);
            if (!path.Add(nextKey))
            {
                continue;
            }

            await TraverseMethodAsync(
                workspace,
                solution,
                repositoryConfigId,
                resolvedMethod,
                nextCompilation,
                nodeId,
                depth + 1,
                nodes,
                path,
                maxDepth,
                cancellationToken);

            path.Remove(nextKey);
        }
    }

    private static IEnumerable<SyntaxNode> GetCallNodes(SyntaxNode root)
    {
        return root.DescendantNodes()
            .Where(node => node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax);
    }

    private static IMethodSymbol? ResolveCallTarget(SemanticModel semanticModel, SyntaxNode callNode)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(callNode);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (symbol is IMethodSymbol method)
        {
            return method;
        }

        if (symbol is IPropertySymbol property && property.GetMethod is not null)
        {
            return property.GetMethod;
        }

        return null;
    }

    private static MethodCallSite BuildCallSite(string rootPath, SyntaxNode callNode)
    {
        var tree = callNode.SyntaxTree;
        var lineSpan = tree.GetLineSpan(callNode.Span);
        var relativePath = SolutionIdentity.NormalizeRelativePath(rootPath, tree.FilePath);

        return new MethodCallSite(
            relativePath,
            callNode.Span.Start,
            callNode.Span.Length,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1,
            lineSpan.EndLinePosition.Line + 1,
            lineSpan.EndLinePosition.Character + 1);
    }

    private static bool TryResolveMethodSymbol(
        CodeSolutionWorkspace solution,
        string methodSymbolKey,
        out IMethodSymbol methodSymbol,
        out Compilation compilation)
    {
        foreach (var compilationEntry in solution.Compilations)
        {
            var candidateCompilation = compilationEntry.Value;
            var resolved = SymbolKeyUtility.ResolveSymbol(methodSymbolKey, candidateCompilation);
            if (resolved is IMethodSymbol method)
            {
                methodSymbol = method;
                compilation = candidateCompilation;
                return true;
            }

            foreach (var candidateMethod in EnumerateMethods(candidateCompilation.GlobalNamespace))
            {
                var candidateKey = SymbolKeyUtility.GetSymbolKeyString(candidateMethod, candidateCompilation);
                if (string.Equals(candidateKey, methodSymbolKey, StringComparison.Ordinal))
                {
                    methodSymbol = candidateMethod;
                    compilation = candidateCompilation;
                    return true;
                }
            }
        }

        methodSymbol = null!;
        compilation = null!;
        return false;
    }

    private static IEnumerable<IMethodSymbol> EnumerateMethods(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                foreach (var nested in EnumerateMethods(ns))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                foreach (var method in EnumerateMethods(type))
                {
                    yield return method;
                }
            }
        }
    }

    private static IEnumerable<IMethodSymbol> EnumerateMethods(INamedTypeSymbol type)
    {
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            yield return method;
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var nestedMethod in EnumerateMethods(nestedType))
            {
                yield return nestedMethod;
            }
        }
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

    private static string BuildFullyQualifiedMethodName(MethodDescriptor descriptor)
    {
        var parameterList = descriptor.ParameterTypes.Count == 0
            ? "none"
            : string.Join(",", descriptor.ParameterTypes);

        return $"{descriptor.Namespace}.{descriptor.TypeName}.{descriptor.MethodName}.{parameterList}";
    }

    private sealed record MethodDescriptor(
        string Namespace,
        string TypeName,
        string MethodName,
        IReadOnlyList<string> ParameterTypes)
    {
        public static MethodDescriptor FromSymbol(IMethodSymbol method)
        {
            var namespaceName = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var typeName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                ?? string.Empty;
            var methodName = method.MethodKind == MethodKind.Constructor
                ? method.ContainingType?.Name ?? method.Name
                : method.Name;
            var parameterTypes = method.Parameters
                .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                .ToList();

            return new MethodDescriptor(namespaceName, typeName, methodName, parameterTypes);
        }
    }
}
