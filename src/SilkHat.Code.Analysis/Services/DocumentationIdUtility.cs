using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services
{
    public static class DocumentationIdUtility
    {
        public static string? GetDocumentationId(ISymbol? symbol)
        {
            if (symbol is null) return null;

            var docId = symbol.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId)) return docId;

            if (symbol is IMethodSymbol method && method.AssociatedSymbol is not null)
                return method.AssociatedSymbol.GetDocumentationCommentId();

            return null;
        }

        public static IMethodSymbol? FindMethodByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            if (documentationId.StartsWith("P:", StringComparison.Ordinal))
            {
                var property = FindPropertyByDocumentationId(solution, documentationId);
                return property?.GetMethod ?? property?.SetMethod;
            }

            if (documentationId.StartsWith("E:", StringComparison.Ordinal))
            {
                var @event = FindEventByDocumentationId(solution, documentationId);
                return @event?.AddMethod ?? @event?.RemoveMethod;
            }

            foreach (var compilation in solution.Compilations.Values)
            foreach (var method in EnumerateMethods(compilation.GlobalNamespace))
            {
                var docId = method.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return method;

                if (method.AssociatedSymbol is not null)
                {
                    var associatedDocId = method.AssociatedSymbol.GetDocumentationCommentId();
                    if (string.Equals(associatedDocId, documentationId, StringComparison.Ordinal)) return method;
                }
            }

            return null;
        }

        public static INamespaceSymbol? FindNamespaceByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var ns in EnumerateNamespaces(compilation.GlobalNamespace))
            {
                var docId = ns.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return ns;
            }

            return null;
        }

        public static INamedTypeSymbol? FindTypeByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            {
                var docId = type.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return type;
            }

            return null;
        }

        public static IPropertySymbol? FindPropertyByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            return FindPropertyByDocumentationIdInternal(solution, documentationId);
        }

        public static IEventSymbol? FindEventByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            return FindEventByDocumentationIdInternal(solution, documentationId);
        }

        public static IFieldSymbol? FindFieldByDocumentationId(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
            {
                var docId = field.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return field;
            }

            return null;
        }

        private static IEnumerable<IMethodSymbol> EnumerateMethods(INamespaceSymbol root)
        {
            foreach (var member in root.GetMembers())
                if (member is INamespaceSymbol ns)
                    foreach (var nested in EnumerateMethods(ns))
                        yield return nested;
                else if (member is INamedTypeSymbol type)
                    foreach (var method in EnumerateMethods(type))
                        yield return method;
        }

        private static IEnumerable<IMethodSymbol> EnumerateMethods(INamedTypeSymbol type)
        {
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>()) yield return method;

            foreach (var nestedType in type.GetTypeMembers())
            foreach (var nestedMethod in EnumerateMethods(nestedType))
                yield return nestedMethod;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
        {
            foreach (var member in root.GetMembers())
                if (member is INamespaceSymbol ns)
                {
                    foreach (var nested in EnumerateTypes(ns)) yield return nested;
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;
                    foreach (var nestedType in type.GetTypeMembers()) yield return nestedType;
                }
        }

        private static IEnumerable<INamespaceSymbol> EnumerateNamespaces(INamespaceSymbol root)
        {
            yield return root;
            foreach (var member in root.GetMembers().OfType<INamespaceSymbol>())
            foreach (var nested in EnumerateNamespaces(member))
                yield return nested;
        }

        private static IPropertySymbol? FindPropertyByDocumentationIdInternal(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                var docId = property.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return property;
            }

            return null;
        }

        private static IEventSymbol? FindEventByDocumentationIdInternal(
            CodeSolutionWorkspace solution,
            string documentationId)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            foreach (var @event in type.GetMembers().OfType<IEventSymbol>())
            {
                var docId = @event.GetDocumentationCommentId();
                if (string.Equals(docId, documentationId, StringComparison.Ordinal)) return @event;
            }

            return null;
        }
    }
}