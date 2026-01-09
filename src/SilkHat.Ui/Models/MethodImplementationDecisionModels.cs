namespace SilkHat.Ui.Models;

public sealed record MethodImplementationDecisionModel(
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

public sealed record MethodImplementationDecisionRequestModel(
    string InterfaceTypeName,
    string? InterfaceTypeDocumentationId,
    string InterfaceMethodSignature,
    string? InterfaceMethodDocumentationId,
    string ImplementationTypeName,
    string? ImplementationTypeDocumentationId,
    string? ImplementationMethodDocumentationId);
