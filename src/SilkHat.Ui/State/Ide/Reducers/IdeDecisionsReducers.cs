using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers
{
    public static class IdeDecisionsReducers
    {
        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadPending(
            IdeDecisionsState state,
            LoadPendingDecisionsAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingPending = true,
                Error = null,
                FilterType = action.FilterType,
                SortOption = action.SortOption,
                SortDescending = action.SortDescending
            };
            return UpdateView(state, action.SolutionId, view);
        }

        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadPendingSuccess(
            IdeDecisionsState state,
            LoadPendingDecisionsSuccessAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingPending = false,
                Pending = action.Decisions
            };
            return UpdateView(state, action.SolutionId, view);
        }

        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadPendingFailure(
            IdeDecisionsState state,
            LoadPendingDecisionsFailureAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingPending = false,
                Error = action.Error
            };
            return UpdateView(state, action.SolutionId, view);
        }

        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadResolved(
            IdeDecisionsState state,
            LoadResolvedDecisionsAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingResolved = true,
                Error = null,
                FilterType = action.FilterType,
                SortOption = action.SortOption,
                SortDescending = action.SortDescending,
                ResolvedActiveFilter = action.ActiveFilter
            };
            return UpdateView(state, action.SolutionId, view);
        }

        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadResolvedSuccess(
            IdeDecisionsState state,
            LoadResolvedDecisionsSuccessAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingResolved = false,
                Resolved = action.Decisions
            };
            return UpdateView(state, action.SolutionId, view);
        }

        [ReducerMethod]
        public static IdeDecisionsState ReduceLoadResolvedFailure(
            IdeDecisionsState state,
            LoadResolvedDecisionsFailureAction action)
        {
            var view = GetView(state, action.SolutionId) with
            {
                IsLoadingResolved = false,
                Error = action.Error
            };
            return UpdateView(state, action.SolutionId, view);
        }

        private static IdeDecisionsViewState GetView(IdeDecisionsState state, string solutionId)
        {
            return state.Views.TryGetValue(solutionId, out var view)
                ? view
                : IdeDecisionsViewState.Default;
        }

        private static IdeDecisionsState UpdateView(
            IdeDecisionsState state,
            string solutionId,
            IdeDecisionsViewState view)
        {
            var views = new Dictionary<string, IdeDecisionsViewState>(state.Views, StringComparer.OrdinalIgnoreCase)
            {
                [solutionId] = view
            };

            return new IdeDecisionsState(views);
        }
    }
}