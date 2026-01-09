using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Actions;

public sealed record LoadPendingDecisionsAction(
    string SolutionId,
    DecisionTypeModel? FilterType,
    DecisionSortOption SortOption,
    bool SortDescending);

public sealed record LoadPendingDecisionsSuccessAction(
    string SolutionId,
    IReadOnlyList<DecisionSummaryModel> Decisions);

public sealed record LoadPendingDecisionsFailureAction(
    string SolutionId,
    string Error);

public sealed record LoadResolvedDecisionsAction(
    string SolutionId,
    DecisionTypeModel? FilterType,
    DecisionSortOption SortOption,
    bool SortDescending,
    bool? ActiveFilter);

public sealed record LoadResolvedDecisionsSuccessAction(
    string SolutionId,
    IReadOnlyList<DecisionSummaryModel> Decisions);

public sealed record LoadResolvedDecisionsFailureAction(
    string SolutionId,
    string Error);

public sealed record DiscoverDecisionsAction(
    string SolutionId,
    DecisionTypeModel? FilterType);

public sealed record ResolveDecisionAction(
    string SolutionId,
    DecisionResolveRequestModel Request);

public sealed record UpdateDecisionNotesAction(
    string SolutionId,
    DecisionNotesRequestModel Request);

public sealed record SetDecisionActiveAction(
    string SolutionId,
    DecisionActivateRequestModel Request);

public sealed record ValidateDecisionAction(
    string SolutionId,
    DecisionValidateRequestModel Request);
