using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers;

public static class IdeLayoutReducers
{
    [ReducerMethod]
    public static IdeLayoutState ReduceSetView(IdeLayoutState state, SetIdeMainViewAction action)
    {
        var views = new Dictionary<string, IdeLayoutViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
        var current = views.TryGetValue(action.SolutionId, out var view)
            ? view
            : IdeLayoutViewState.Default;
        views[action.SolutionId] = current with { ActiveView = action.View };
        return new IdeLayoutState(views);
    }

    [ReducerMethod]
    public static IdeLayoutState ReduceToolboxVisibility(IdeLayoutState state, SetToolboxVisibilityAction action)
    {
        var views = new Dictionary<string, IdeLayoutViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
        var current = views.TryGetValue(action.SolutionId, out var view)
            ? view
            : IdeLayoutViewState.Default;
        views[action.SolutionId] = current with { IsToolboxHidden = action.IsHidden };
        return new IdeLayoutState(views);
    }
}
