namespace SilkHat.Ui.State.Ide;

public enum IdeMainView
{
    Tabs
}

public sealed record IdeLayoutState
{
    public IdeLayoutState()
    {
        Views = new Dictionary<string, IdeLayoutViewState>(StringComparer.OrdinalIgnoreCase);
    }

    public IdeLayoutState(IDictionary<string, IdeLayoutViewState> views)
    {
        Views = new Dictionary<string, IdeLayoutViewState>(views, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IdeLayoutViewState> Views { get; init; }
}

public sealed record IdeLayoutViewState(IdeMainView ActiveView, bool IsToolboxHidden)
{
    public static IdeLayoutViewState Default => new(IdeMainView.Tabs, false);
}
