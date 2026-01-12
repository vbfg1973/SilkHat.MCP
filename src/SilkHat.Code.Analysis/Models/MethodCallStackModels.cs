namespace SilkHat.Code.Analysis.Models
{
    public sealed record MethodCallStackNode(
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
        MethodCallSite CallSite,
        DecisionUsage? Decision,
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

    public sealed record MethodCallSite(
        string Path,
        int SpanStart,
        int SpanLength,
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn);

    public sealed record DecisionUsage(Guid Id, string Type);

    public sealed record MethodCallStackResult(
        IReadOnlyList<MethodCallStackNode> Nodes,
        bool IsPartial,
        string? Error);
}