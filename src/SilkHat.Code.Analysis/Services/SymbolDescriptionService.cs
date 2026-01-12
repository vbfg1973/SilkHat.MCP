using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class SymbolDescriptionService : ISymbolDescriptionService
    {
        public Task<SymbolDescriptionResult<NamedTypeDescriptionDto>> DescribeNamedTypeAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<NamedTypeDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            var symbol = DocumentationIdUtility.FindTypeByDocumentationId(solution, documentationId);
            if (symbol is null)
                return Task.FromResult(new SymbolDescriptionResult<NamedTypeDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Named type not found."));

            var modifiers = BuildModifiers(symbol);
            var dto = new NamedTypeDescriptionDto(
                documentationId,
                symbol.Name,
                symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                GetNamedTypeKind(symbol),
                symbol.DeclaredAccessibility.ToString(),
                symbol.IsGenericType,
                symbol.TypeParameters.Select(parameter => parameter.Name).ToList(),
                modifiers,
                BuildLocation(workspace.RootPath, symbol));

            return Task.FromResult(new SymbolDescriptionResult<NamedTypeDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        public Task<SymbolDescriptionResult<MethodDescriptionDto>> DescribeMethodAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<MethodDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            if (!documentationId.StartsWith("M:", StringComparison.Ordinal))
                return Task.FromResult(new SymbolDescriptionResult<MethodDescriptionDto>(
                    SymbolDescriptionStatus.Unsupported,
                    null,
                    "DocumentationId does not describe a method."));

            var method = DocumentationIdUtility.FindMethodByDocumentationId(solution, documentationId);
            if (method is null)
                return Task.FromResult(new SymbolDescriptionResult<MethodDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Method not found."));

            var modifiers = BuildModifiers(method);
            var dto = new MethodDescriptionDto(
                documentationId,
                method.Name,
                method.ContainingType?.Name ?? string.Empty,
                method.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.DeclaredAccessibility.ToString(),
                modifiers,
                method.Parameters.Select(BuildParameter).ToList(),
                method.TypeParameters.Select(parameter => parameter.Name).ToList(),
                BuildLocation(workspace.RootPath, method));

            return Task.FromResult(new SymbolDescriptionResult<MethodDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        public Task<SymbolDescriptionResult<PropertyDescriptionDto>> DescribePropertyAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<PropertyDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            if (!documentationId.StartsWith("P:", StringComparison.Ordinal))
                return Task.FromResult(new SymbolDescriptionResult<PropertyDescriptionDto>(
                    SymbolDescriptionStatus.Unsupported,
                    null,
                    "DocumentationId does not describe a property."));

            var property = DocumentationIdUtility.FindPropertyByDocumentationId(solution, documentationId);
            if (property is null)
                return Task.FromResult(new SymbolDescriptionResult<PropertyDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Property not found."));

            var modifiers = BuildModifiers(property);
            var dto = new PropertyDescriptionDto(
                documentationId,
                property.Name,
                property.ContainingType?.Name ?? string.Empty,
                property.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                property.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                property.DeclaredAccessibility.ToString(),
                property.GetMethod is not null,
                property.SetMethod is not null,
                property.IsIndexer,
                modifiers,
                BuildLocation(workspace.RootPath, property));

            return Task.FromResult(new SymbolDescriptionResult<PropertyDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        public Task<SymbolDescriptionResult<FieldDescriptionDto>> DescribeFieldAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<FieldDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            if (!documentationId.StartsWith("F:", StringComparison.Ordinal))
                return Task.FromResult(new SymbolDescriptionResult<FieldDescriptionDto>(
                    SymbolDescriptionStatus.Unsupported,
                    null,
                    "DocumentationId does not describe a field."));

            var field = DocumentationIdUtility.FindFieldByDocumentationId(solution, documentationId);
            if (field is null)
                return Task.FromResult(new SymbolDescriptionResult<FieldDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Field not found."));

            var modifiers = BuildModifiers(field);
            var dto = new FieldDescriptionDto(
                documentationId,
                field.Name,
                field.ContainingType?.Name ?? string.Empty,
                field.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                field.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                field.DeclaredAccessibility.ToString(),
                modifiers,
                BuildLocation(workspace.RootPath, field));

            return Task.FromResult(new SymbolDescriptionResult<FieldDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        public Task<SymbolDescriptionResult<EventDescriptionDto>> DescribeEventAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<EventDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            if (!documentationId.StartsWith("E:", StringComparison.Ordinal))
                return Task.FromResult(new SymbolDescriptionResult<EventDescriptionDto>(
                    SymbolDescriptionStatus.Unsupported,
                    null,
                    "DocumentationId does not describe an event."));

            var @event = DocumentationIdUtility.FindEventByDocumentationId(solution, documentationId);
            if (@event is null)
                return Task.FromResult(new SymbolDescriptionResult<EventDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Event not found."));

            var modifiers = BuildModifiers(@event);
            var dto = new EventDescriptionDto(
                documentationId,
                @event.Name,
                @event.ContainingType?.Name ?? string.Empty,
                @event.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                @event.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                @event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                @event.DeclaredAccessibility.ToString(),
                modifiers,
                BuildLocation(workspace.RootPath, @event));

            return Task.FromResult(new SymbolDescriptionResult<EventDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        public Task<SymbolDescriptionResult<NamespaceDescriptionDto>> DescribeNamespaceAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(documentationId))
                return Task.FromResult(new SymbolDescriptionResult<NamespaceDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "DocumentationId is required."));

            if (!documentationId.StartsWith("N:", StringComparison.Ordinal))
                return Task.FromResult(new SymbolDescriptionResult<NamespaceDescriptionDto>(
                    SymbolDescriptionStatus.Unsupported,
                    null,
                    "DocumentationId does not describe a namespace."));

            var symbol = DocumentationIdUtility.FindNamespaceByDocumentationId(solution, documentationId);
            if (symbol is null)
                return Task.FromResult(new SymbolDescriptionResult<NamespaceDescriptionDto>(
                    SymbolDescriptionStatus.NotFound,
                    null,
                    "Namespace not found."));

            var dto = new NamespaceDescriptionDto(
                documentationId,
                symbol.Name,
                symbol.ToDisplayString(),
                symbol.IsGlobalNamespace,
                BuildLocation(workspace.RootPath, symbol));

            return Task.FromResult(new SymbolDescriptionResult<NamespaceDescriptionDto>(
                SymbolDescriptionStatus.Success,
                dto,
                null));
        }

        private static SymbolModifiersDto BuildModifiers(ISymbol symbol)
        {
            return symbol switch
            {
                INamedTypeSymbol type => new SymbolModifiersDto(
                    type.IsAbstract,
                    type.IsSealed,
                    type.IsStatic,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    type.IsPartial()),
                IMethodSymbol method => new SymbolModifiersDto(
                    method.IsAbstract,
                    false,
                    method.IsStatic,
                    method.IsVirtual,
                    method.IsOverride,
                    method.IsAsync,
                    method.IsExtern,
                    false,
                    false,
                    method.PartialDefinitionPart is not null || method.PartialImplementationPart is not null),
                IPropertySymbol property => new SymbolModifiersDto(
                    property.IsAbstract,
                    false,
                    property.IsStatic,
                    property.IsVirtual,
                    property.IsOverride,
                    false,
                    property.IsExtern,
                    property.IsReadOnly,
                    false,
                    false),
                IFieldSymbol field => new SymbolModifiersDto(
                    false,
                    false,
                    field.IsStatic,
                    false,
                    false,
                    false,
                    false,
                    field.IsReadOnly,
                    field.IsConst,
                    false),
                IEventSymbol @event => new SymbolModifiersDto(
                    @event.IsAbstract,
                    false,
                    @event.IsStatic,
                    @event.IsVirtual,
                    @event.IsOverride,
                    false,
                    @event.IsExtern,
                    false,
                    false,
                    false),
                _ => new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false)
            };
        }

        private static SymbolParameterDto BuildParameter(IParameterSymbol parameter)
        {
            return new SymbolParameterDto(
                parameter.Name,
                parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                parameter.RefKind.ToString(),
                parameter.IsOptional);
        }

        private static string GetNamedTypeKind(INamedTypeSymbol symbol)
        {
            if (symbol.IsRecord) return symbol.TypeKind == TypeKind.Struct ? "Struct" : "Record";

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

        private static CodeLocationDto? BuildLocation(string rootPath, ISymbol symbol)
        {
            var location = symbol.Locations.FirstOrDefault(candidate => candidate.IsInSource);
            if (location is null) return null;

            var lineSpan = location.GetLineSpan();
            var fullPath = location.SourceTree?.FilePath ?? lineSpan.Path;
            var relativePath = SolutionIdentity.NormalizeRelativePath(rootPath, fullPath);
            var span = location.SourceSpan;

            return new CodeLocationDto(
                relativePath,
                new CodeTextSpanDto(span.Start, span.Length),
                new CodeLineSpanDto(
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Character + 1));
        }
    }

    internal static class SymbolExtensions
    {
        public static bool IsPartial(this INamedTypeSymbol symbol)
        {
            return symbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(node => node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
        }
    }
}