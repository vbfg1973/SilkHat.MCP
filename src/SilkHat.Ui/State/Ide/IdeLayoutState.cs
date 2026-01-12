namespace SilkHat.Ui.State.Ide
{
    public enum IdeMainView
    {
        Tabs
    }

    public enum IdeToolboxView
    {
        None,
        Decisions
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

    public sealed record IdeLayoutViewState(IdeMainView ActiveView, IdeToolboxView ToolboxView)
    {
        public static IdeLayoutViewState Default => new(IdeMainView.Tabs, IdeToolboxView.None);
    }
}