namespace SilkHat.Infrastructure.Entities;

public sealed class MethodImplementationDecision
{
    public Guid Id { get; set; }
    public Guid RepositoryConfigId { get; set; }
    public string SolutionId { get; set; } = string.Empty;
    public string InterfaceTypeName { get; set; } = string.Empty;
    public string? InterfaceTypeDocumentationId { get; set; }
    public string InterfaceMethodSignature { get; set; } = string.Empty;
    public string? InterfaceMethodDocumentationId { get; set; }
    public string ImplementationTypeName { get; set; } = string.Empty;
    public string? ImplementationTypeDocumentationId { get; set; }
    public string? ImplementationMethodDocumentationId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
