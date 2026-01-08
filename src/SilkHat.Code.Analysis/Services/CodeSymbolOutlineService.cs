using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeSymbolOutlineService : ICodeSymbolOutlineService
{
    public Task<CodeFileSymbolsResult> GetFileSymbolsAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string repositoryPath,
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

        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return Task.FromResult(new CodeFileSymbolsResult(
                CodeFileSymbolsStatus.InvalidPath,
                null,
                "Path cannot be empty."));
        }

        var entry = solution.TreeEntries.FirstOrDefault(item =>
            item.Type == CodeTreeEntryType.File
            && (string.Equals(item.RepositoryPath, repositoryPath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.DisplayPath, repositoryPath, StringComparison.OrdinalIgnoreCase)));
        if (entry is null)
        {
            return Task.FromResult(new CodeFileSymbolsResult(
                CodeFileSymbolsStatus.NotFound,
                null,
                "File not found in solution."));
        }

        if (!TryResolvePath(workspace.RootPath, entry.RepositoryPath, out var fullPath))
        {
            return Task.FromResult(new CodeFileSymbolsResult(
                CodeFileSymbolsStatus.InvalidPath,
                null,
                "Invalid file path."));
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(new CodeFileSymbolsResult(
                CodeFileSymbolsStatus.NotFound,
                null,
                "File not found on disk."));
        }

        var result = BuildOutline(solution, fullPath, cancellationToken);
        return Task.FromResult(result);
    }

    private static CodeFileSymbolsResult BuildOutline(
        CodeSolutionWorkspace solution,
        string fullPath,
        CancellationToken cancellationToken)
    {
        var normalizedFullPath = Path.GetFullPath(fullPath);
        foreach (var compilationEntry in solution.Compilations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = compilationEntry.Value;
            var tree = compilation.SyntaxTrees.FirstOrDefault(candidate =>
                PathsEqual(candidate.FilePath, normalizedFullPath));
            if (tree is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetRoot(cancellationToken);
            var typeSymbols = FindTopLevelNamedTypes(root, semanticModel);
            var nodes = typeSymbols
                .Select(symbol => BuildTypeNode(symbol, compilation))
                .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CodeFileSymbolsResult(CodeFileSymbolsStatus.Success, nodes, null);
        }

        return new CodeFileSymbolsResult(
            CodeFileSymbolsStatus.NotFound,
            null,
            "No symbols were found for the file.");
    }

    private static IEnumerable<INamedTypeSymbol> FindTopLevelNamedTypes(SyntaxNode root, SemanticModel semanticModel)
    {
        var candidates = root.DescendantNodes()
            .Where(node => node is BaseTypeDeclarationSyntax
                           || node is EnumDeclarationSyntax
                           || node is DelegateDeclarationSyntax)
            .Select(node => semanticModel.GetDeclaredSymbol(node))
            .OfType<INamedTypeSymbol>()
            .Where(symbol => symbol.ContainingType is null)
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<INamedTypeSymbol>();

        return candidates;
    }

    private static SymbolOutlineNodeDto BuildTypeNode(INamedTypeSymbol symbol, Compilation compilation)
    {
        var children = new List<SymbolOutlineNodeDto>();
        foreach (var member in symbol.GetMembers().Where(item => !item.IsImplicitlyDeclared))
        {
            switch (member)
            {
                case INamedTypeSymbol nestedType:
                    children.Add(BuildTypeNode(nestedType, compilation));
                    break;
                case IMethodSymbol method:
                    if (ShouldIncludeMethod(method))
                    {
                        children.Add(BuildMemberNode(method, compilation));
                    }
                    break;
                case IPropertySymbol property:
                    children.Add(BuildMemberNode(property, compilation));
                    break;
                case IFieldSymbol field:
                    children.Add(BuildMemberNode(field, compilation));
                    break;
                case IEventSymbol eventSymbol:
                    children.Add(BuildMemberNode(eventSymbol, compilation));
                    break;
            }
        }

        var orderedChildren = children
            .OrderBy(child => child.SymbolKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SymbolOutlineNodeDto(
            SymbolKeyUtility.GetSymbolKeyString(symbol, compilation),
            DocumentationIdUtility.GetDocumentationId(symbol),
            symbol.Name,
            symbol.Kind.ToString(),
            GetRealType(symbol),
            orderedChildren);
    }

    private static SymbolOutlineNodeDto BuildMemberNode(ISymbol symbol, Compilation compilation)
    {
        return new SymbolOutlineNodeDto(
            SymbolKeyUtility.GetSymbolKeyString(symbol, compilation),
            DocumentationIdUtility.GetDocumentationId(symbol),
            symbol.Name,
            symbol.Kind.ToString(),
            GetRealType(symbol),
            Array.Empty<SymbolOutlineNodeDto>());
    }

    private static bool ShouldIncludeMethod(IMethodSymbol method)
    {
        return method.MethodKind switch
        {
            MethodKind.PropertyGet => false,
            MethodKind.PropertySet => false,
            MethodKind.EventAdd => false,
            MethodKind.EventRemove => false,
            MethodKind.EventRaise => false,
            _ => true
        };
    }

    private static string GetRealType(ISymbol symbol)
    {
        return symbol switch
        {
            INamedTypeSymbol namedType => GetNamedTypeKind(namedType),
            IMethodSymbol method => GetMethodKind(method),
            IPropertySymbol => "Property",
            IFieldSymbol => "Field",
            IEventSymbol => "Event",
            _ => symbol.Kind.ToString()
        };
    }

    private static string GetNamedTypeKind(INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
        {
            return "Record";
        }

        return symbol.TypeKind switch
        {
            TypeKind.Class => "Class",
            TypeKind.Struct => "Struct",
            TypeKind.Interface => "Interface",
            TypeKind.Enum => "Enum",
            TypeKind.Delegate => "Delegate",
            _ => "Unknown"
        };
    }

    private static string GetMethodKind(IMethodSymbol method)
    {
        return method.MethodKind switch
        {
            MethodKind.Constructor => "Constructor",
            MethodKind.StaticConstructor => "Constructor",
            MethodKind.Destructor => "Destructor",
            _ => "Method"
        };
    }

    private static bool TryResolvePath(string repositoryRoot, string repositoryPath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(repositoryPath))
        {
            return false;
        }

        var rootFullPath = Path.GetFullPath(repositoryRoot);
        var relativePath = repositoryPath.Replace('\\', '/');
        if (relativePath.StartsWith("./", StringComparison.Ordinal))
        {
            relativePath = relativePath[2..];
        }

        relativePath = relativePath.TrimStart('/');
        var combined = Path.Combine(rootFullPath, relativePath);
        var resolved = Path.GetFullPath(combined);
        var relative = Path.GetRelativePath(rootFullPath, resolved);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return false;
        }

        fullPath = resolved;
        return true;
    }

    private static bool PathsEqual(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftFull = Path.GetFullPath(left);
        var rightFull = Path.GetFullPath(right);
        return string.Equals(leftFull, rightFull, StringComparison.OrdinalIgnoreCase);
    }
}
