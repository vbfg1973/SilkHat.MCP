using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers;

public static class IdeCallStackReducers
{
    [ReducerMethod]
    public static IdeCallStackState ReduceOpen(IdeCallStackState state, OpenCallStackPopupAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsOpen = true,
            Error = null,
            MermaidError = null,
            SymbolKey = action.SymbolKey
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceClose(IdeCallStackState state, CloseCallStackPopupAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsOpen = false,
            IsLoading = false,
            IsMermaidLoading = false,
            Error = null,
            MermaidError = null
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceLoad(IdeCallStackState state, LoadCallStackAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsLoading = true,
            Error = null,
            SymbolKey = action.SymbolKey
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceLoadSuccess(IdeCallStackState state, LoadCallStackSuccessAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsLoading = false,
            Error = null,
            SymbolKey = action.SymbolKey,
            Nodes = action.Nodes
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceLoadFailure(IdeCallStackState state, LoadCallStackFailureAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsLoading = false,
            Error = action.Error
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceMermaidLoad(IdeCallStackState state, LoadCallStackMermaidAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsMermaidLoading = true,
            MermaidError = null,
            SymbolKey = action.SymbolKey
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceMermaidSuccess(IdeCallStackState state, LoadCallStackMermaidSuccessAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsMermaidLoading = false,
            MermaidError = null,
            MermaidDiagram = action.Diagram
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceMermaidFailure(IdeCallStackState state, LoadCallStackMermaidFailureAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with
        {
            IsMermaidLoading = false,
            MermaidError = action.Error
        };

        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceSaveDecisionSuccess(IdeCallStackState state, SaveCallStackDecisionSuccessAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with { Error = null };
        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceSaveDecisionFailure(IdeCallStackState state, SaveCallStackDecisionFailureAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with { Error = action.Error };
        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceSelectNode(IdeCallStackState state, SelectCallStackNodeAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with { SelectedNodeId = action.NodeId };
        return state with { Views = views };
    }

    [ReducerMethod]
    public static IdeCallStackState ReduceShading(IdeCallStackState state, SetCallStackShadingModeAction action)
    {
        var views = state.Views.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var view = views.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : IdeCallStackViewState.Empty;

        views[action.SolutionId] = view with { ShadingMode = action.Mode };
        return state with { Views = views };
    }
}
