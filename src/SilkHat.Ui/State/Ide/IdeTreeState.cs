using MudBlazor;
using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide
{
    public sealed record IdeTreeState
    {
        public IdeTreeState()
        {
            Trees = new Dictionary<string, IdeTreeViewState>(StringComparer.OrdinalIgnoreCase);
        }

        public IdeTreeState(IDictionary<string, IdeTreeViewState> trees)
        {
            Trees = new Dictionary<string, IdeTreeViewState>(trees, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, IdeTreeViewState> Trees { get; init; }
    }

    public sealed record IdeTreeViewState(
        bool IsLoading,
        bool RootLoaded,
        string? Error,
        List<TreeItemData<CodeTreeEntryModel>> Items)
    {
        public static IdeTreeViewState Empty => new(false, false, null, new List<TreeItemData<CodeTreeEntryModel>>());
    }
}