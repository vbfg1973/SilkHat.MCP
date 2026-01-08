using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide;

public sealed record IdeTabsState
{
    public IdeTabsState()
    {
        Tabs = new Dictionary<string, IdeTabsViewState>(StringComparer.OrdinalIgnoreCase);
    }

    public IdeTabsState(IDictionary<string, IdeTabsViewState> tabs)
    {
        Tabs = new Dictionary<string, IdeTabsViewState>(tabs, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IdeTabsViewState> Tabs { get; init; }
}

public sealed record IdeTabsViewState(
    List<IdeOpenFileTab> OpenFiles,
    int ActiveTabIndex,
    string? Error)
{
    public static IdeTabsViewState Empty => new(new List<IdeOpenFileTab>(), 0, null);
}
