using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide;

public sealed record IdeSolutionsState
{
    public IdeSolutionsState()
    {
        Solutions = Array.Empty<IdeSolutionEntry>();
    }

    public IdeSolutionsState(
        bool isLoading,
        string? error,
        IReadOnlyList<IdeSolutionEntry> solutions,
        string? selectedSolutionId)
    {
        IsLoading = isLoading;
        Error = error;
        Solutions = solutions;
        SelectedSolutionId = selectedSolutionId;
    }

    public bool IsLoading { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<IdeSolutionEntry> Solutions { get; init; }
    public string? SelectedSolutionId { get; init; }
}
