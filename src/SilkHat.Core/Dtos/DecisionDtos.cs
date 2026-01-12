using System.Text.Json;

namespace SilkHat.Core.Dtos
{
    public enum DecisionType
    {
        ResolveInterface = 1
    }

    public enum DecisionStatus
    {
        Pending = 1,
        Resolved = 2
    }

    public sealed record DecisionSummaryDto(
        Guid Id,
        DecisionType Type,
        DecisionStatus Status,
        bool IsActive,
        bool IsValid,
        string Name,
        string SubjectKey,
        DateTimeOffset DiscoveredUtc,
        DateTimeOffset? ResolvedUtc,
        string? Notes,
        object? Payload);

    public sealed record ResolveInterfaceDecisionCandidateDto(
        string TypeName,
        string? TypeDocId,
        string? MethodDocId);

    public sealed record ResolveInterfaceDecisionPayloadDto(
        string InterfaceTypeName,
        string InterfaceTypeDocId,
        string InterfaceMethodDocId,
        IReadOnlyList<ResolveInterfaceDecisionCandidateDto> Candidates,
        string? SelectedTypeDocId,
        string? SelectedMethodDocId);

    public sealed record DecisionResolveRequestDto(
        Guid DecisionId,
        DecisionType Type,
        JsonElement Payload);

    public sealed record DecisionNotesRequestDto(
        Guid DecisionId,
        string? Notes);

    public sealed record DecisionActivateRequestDto(
        Guid DecisionId,
        bool IsActive);

    public sealed record DecisionValidateRequestDto(
        Guid DecisionId);

    public sealed record DecisionDiscoverRequestDto(
        DecisionType? Type);
}