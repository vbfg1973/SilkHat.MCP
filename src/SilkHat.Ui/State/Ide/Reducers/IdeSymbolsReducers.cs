using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers
{
    public static class IdeSymbolsReducers
    {
        [ReducerMethod]
        public static IdeSymbolsState ReduceOpenPopup(IdeSymbolsState state, OpenSymbolPopupAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with { IsOpen = true, Error = null };
            return new IdeSymbolsState(views);
        }

        [ReducerMethod]
        public static IdeSymbolsState ReduceClosePopup(IdeSymbolsState state, CloseSymbolPopupAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with { IsOpen = false };
            return new IdeSymbolsState(views);
        }

        [ReducerMethod]
        public static IdeSymbolsState ReduceLoadSymbols(IdeSymbolsState state, LoadFileSymbolsAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with
            {
                IsOpen = true,
                IsLoading = true,
                Error = null,
                RepositoryPath = action.RepositoryPath
            };
            return new IdeSymbolsState(views);
        }

        [ReducerMethod]
        public static IdeSymbolsState ReduceLoadSymbolsSuccess(IdeSymbolsState state,
            LoadFileSymbolsSuccessAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with
            {
                IsOpen = true,
                IsLoading = false,
                Error = null,
                RepositoryPath = action.RepositoryPath,
                Nodes = action.Nodes
            };
            return new IdeSymbolsState(views);
        }

        [ReducerMethod]
        public static IdeSymbolsState ReduceLoadSymbolsFailure(IdeSymbolsState state,
            LoadFileSymbolsFailureAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with
            {
                IsOpen = true,
                IsLoading = false,
                Error = action.Error,
                RepositoryPath = action.RepositoryPath
            };
            return new IdeSymbolsState(views);
        }

        [ReducerMethod]
        public static IdeSymbolsState ReduceSelectSymbol(IdeSymbolsState state, SelectSymbolNodeAction action)
        {
            var views = new Dictionary<string, IdeSymbolsViewState>(state.Views, StringComparer.OrdinalIgnoreCase);
            var view = views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            views[action.SolutionId] = view with
            {
                SelectedDocumentationId = action.DocumentationId,
                SelectedSymbolKey = action.SymbolKey
            };
            return new IdeSymbolsState(views);
        }
    }
}