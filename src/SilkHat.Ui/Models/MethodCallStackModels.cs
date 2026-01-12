namespace SilkHat.Ui.Models
{
    public sealed record CodeTextSpanModel(int Start, int Length);

    public sealed record CodeLineSpanModel(
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn);

    public sealed record CodeLocationModel(
        string Path,
        CodeTextSpanModel Span,
        CodeLineSpanModel LineSpan);

    public sealed record DecisionInfoModel(Guid Id, string Type);

    public sealed record MethodCallStackRequestModel(
        string? DocumentationId,
        string SymbolKey,
        int? MaxDepth,
        bool IncludeExternalCalls);

    public sealed record MethodCallStackNodeModel(
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
        CodeLocationModel CallSite,
        DecisionInfoModel? Decision,
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

    public sealed record MethodCallStackResponseModel(
        IReadOnlyList<MethodCallStackNodeModel> Nodes);

    public sealed record MethodCallStackMermaidModel(string Diagram);
}