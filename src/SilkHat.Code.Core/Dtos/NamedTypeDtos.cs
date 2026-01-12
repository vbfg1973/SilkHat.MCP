namespace SilkHat.Code.Core.Dtos
{
    public enum NamedTypeKind
    {
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
        Record,
        Unknown
    }

    public sealed record NamedTypeDto(
        string SymbolKey,
        string? DocumentationId,
        string Name,
        string Namespace,
        string FullName,
        string ProjectKey,
        string AssemblyName,
        NamedTypeKind Kind,
        bool IsExternal,
        string? FilePath);

    public sealed record NamedTypeQuery(
        string? PathPrefix,
        string? NamespacePrefix,
        string? NameContains,
        NamedTypeKind? Kind,
        bool? DefinedOnly);
}