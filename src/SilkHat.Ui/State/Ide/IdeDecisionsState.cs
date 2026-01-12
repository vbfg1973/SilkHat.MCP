using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide
{
    public enum DecisionSortOption
    {
        Age = 1,
        Type = 2
    }

    public sealed record IdeDecisionsViewState(
        bool IsLoadingPending,
        bool IsLoadingResolved,
        string? Error,
        DecisionTypeModel? FilterType,
        DecisionSortOption SortOption,
        bool SortDescending,
        bool? ResolvedActiveFilter,
        IReadOnlyList<DecisionSummaryModel> Pending,
        IReadOnlyList<DecisionSummaryModel> Resolved)
    {
        public static IdeDecisionsViewState Default { get; } = new(
            false,
            false,
            null,
            null,
            DecisionSortOption.Age,
            false,
            null,
            Array.Empty<DecisionSummaryModel>(),
            Array.Empty<DecisionSummaryModel>());
    }

    public sealed record IdeDecisionsState(
        IReadOnlyDictionary<string, IdeDecisionsViewState> Views)
    {
        public IdeDecisionsState()
            : this(new Dictionary<string, IdeDecisionsViewState>(StringComparer.OrdinalIgnoreCase))
        {
        }
    }
}