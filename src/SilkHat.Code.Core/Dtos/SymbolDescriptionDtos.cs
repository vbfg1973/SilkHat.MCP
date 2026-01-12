namespace SilkHat.Code.Core.Dtos
{
    public sealed record SymbolModifiersDto(
        bool IsAbstract,
        bool IsSealed,
        bool IsStatic,
        bool IsVirtual,
        bool IsOverride,
        bool IsAsync,
        bool IsExtern,
        bool IsReadOnly,
        bool IsConst,
        bool IsPartial);

    public sealed record SymbolParameterDto(
        string Name,
        string TypeName,
        string RefKind,
        bool IsOptional);

    public sealed record NamedTypeDescriptionDto(
        string DocumentationId,
        string Name,
        string Namespace,
        string FullName,
        string TypeKind,
        string Accessibility,
        bool IsGeneric,
        IReadOnlyList<string> TypeParameters,
        SymbolModifiersDto Modifiers,
        CodeLocationDto? Location);

    public sealed record MethodDescriptionDto(
        string DocumentationId,
        string Name,
        string ContainingType,
        string ContainingNamespace,
        string FullyQualifiedName,
        string ReturnType,
        string Accessibility,
        SymbolModifiersDto Modifiers,
        IReadOnlyList<SymbolParameterDto> Parameters,
        IReadOnlyList<string> TypeParameters,
        CodeLocationDto? Location);

    public sealed record PropertyDescriptionDto(
        string DocumentationId,
        string Name,
        string ContainingType,
        string ContainingNamespace,
        string FullyQualifiedName,
        string PropertyType,
        string Accessibility,
        bool HasGetter,
        bool HasSetter,
        bool IsIndexer,
        SymbolModifiersDto Modifiers,
        CodeLocationDto? Location);

    public sealed record FieldDescriptionDto(
        string DocumentationId,
        string Name,
        string ContainingType,
        string ContainingNamespace,
        string FullyQualifiedName,
        string FieldType,
        string Accessibility,
        SymbolModifiersDto Modifiers,
        CodeLocationDto? Location);

    public sealed record EventDescriptionDto(
        string DocumentationId,
        string Name,
        string ContainingType,
        string ContainingNamespace,
        string FullyQualifiedName,
        string EventType,
        string Accessibility,
        SymbolModifiersDto Modifiers,
        CodeLocationDto? Location);

    public sealed record NamespaceDescriptionDto(
        string DocumentationId,
        string Name,
        string FullName,
        bool IsGlobalNamespace,
        CodeLocationDto? Location);
}