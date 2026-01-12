namespace SilkHat.Code.Core.Dtos
{
    public sealed record MethodImplementationDecisionDto(
        Guid Id,
        string InterfaceTypeName,
        string? InterfaceTypeDocumentationId,
        string InterfaceMethodSignature,
        string? InterfaceMethodDocumentationId,
        string ImplementationTypeName,
        string? ImplementationTypeDocumentationId,
        string? ImplementationMethodDocumentationId,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc);

    public sealed record MethodImplementationDecisionRequestDto(
        string InterfaceTypeName,
        string? InterfaceTypeDocumentationId,
        string InterfaceMethodSignature,
        string? InterfaceMethodDocumentationId,
        string ImplementationTypeName,
        string? ImplementationTypeDocumentationId,
        string? ImplementationMethodDocumentationId);
}