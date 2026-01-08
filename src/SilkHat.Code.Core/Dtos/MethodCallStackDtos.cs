namespace SilkHat.Code.Core.Dtos;

public sealed record MethodCallStackRequestDto(
    string SymbolKey,
    int? MaxDepth);

public sealed record DecisionInfoDto(Guid Id, string Type);

public sealed record MethodCallStackNodeDto(
    string NodeId,
    int Depth,
    int Index,
    string CallerNamespace,
    string CallerTypeName,
    string CallerMethodName,
    IReadOnlyList<string> CallerParameterTypes,
    string CallerFullyQualifiedMethodName,
    string Namespace,
    string TypeName,
    string MethodName,
    IReadOnlyList<string> ParameterTypes,
    string FullyQualifiedMethodName,
    string? ParentNodeId,
    CodeLocationDto CallSite,
    DecisionInfoDto? Decision,
    bool IsInterfaceTarget,
    string? InterfaceTypeName,
    string? InterfaceTypeDocumentationId,
    string? InterfaceMethodDocumentationId,
    string? ResolvedTypeName,
    string? ResolvedTypeDocumentationId,
    string? ResolvedMethodDocumentationId,
    bool DecisionRequired,
    IReadOnlyList<string> CandidateTypeNames,
    IReadOnlyList<string?> CandidateMethodDocumentationIds);

public sealed record MethodCallStackResponseDto(
    IReadOnlyList<MethodCallStackNodeDto> Nodes);

public sealed record MethodCallStackMermaidDto(string Diagram);
