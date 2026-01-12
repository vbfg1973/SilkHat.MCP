using System.Text.Json;

namespace SilkHat.Ui.Models
{
    public enum DecisionTypeModel
    {
        ResolveInterface = 1
    }

    public enum DecisionStatusModel
    {
        Pending = 1,
        Resolved = 2
    }

    public sealed record DecisionSummaryModel(
        Guid Id,
        DecisionTypeModel Type,
        DecisionStatusModel Status,
        bool IsActive,
        bool IsValid,
        string Name,
        string SubjectKey,
        DateTimeOffset DiscoveredUtc,
        DateTimeOffset? ResolvedUtc,
        string? Notes,
        JsonElement? Payload);

    public sealed record ResolveInterfaceDecisionCandidateModel(
        string TypeName,
        string? TypeDocId,
        string? MethodDocId);

    public sealed record ResolveInterfaceDecisionPayloadModel(
        string InterfaceTypeName,
        string InterfaceTypeDocId,
        string InterfaceMethodDocId,
        IReadOnlyList<ResolveInterfaceDecisionCandidateModel> Candidates,
        string? SelectedTypeDocId,
        string? SelectedMethodDocId);

    public sealed record DecisionResolveRequestModel(
        Guid DecisionId,
        DecisionTypeModel Type,
        object Payload);

    public sealed record DecisionNotesRequestModel(
        Guid DecisionId,
        string? Notes);

    public sealed record DecisionActivateRequestModel(
        Guid DecisionId,
        bool IsActive);

    public sealed record DecisionValidateRequestModel(
        Guid DecisionId);

    public sealed record DecisionDiscoverRequestModel(
        DecisionTypeModel? Type);
}