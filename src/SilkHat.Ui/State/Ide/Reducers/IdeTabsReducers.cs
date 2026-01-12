using Fluxor;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Reducers
{
    public static class IdeTabsReducers
    {
        [ReducerMethod]
        public static IdeTabsState ReduceOpenFileSuccess(
            IdeTabsState state,
            OpenFileTabSuccessAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            var view = tabs.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeTabsViewState.Empty;

            var files = view.OpenFiles.ToList();
            var existingIndex = files.FindIndex(file =>
                string.Equals(file.RepositoryPath, action.Tab.RepositoryPath, StringComparison.OrdinalIgnoreCase));
            var activeIndex = view.ActiveTabIndex;
            if (existingIndex >= 0)
            {
                activeIndex = existingIndex;
            }
            else
            {
                files.Add(action.Tab);
                activeIndex = files.Count - 1;
            }

            tabs[action.SolutionId] = view with { OpenFiles = files, ActiveTabIndex = activeIndex, Error = null };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceOpenFileFailure(
            IdeTabsState state,
            OpenFileTabFailureAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            var view = tabs.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeTabsViewState.Empty;
            tabs[action.SolutionId] = view with { Error = action.Error };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceCloseTab(
            IdeTabsState state,
            CloseFileTabAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            if (!tabs.TryGetValue(action.SolutionId, out var view)) return state;

            if (action.Index < 0 || action.Index >= view.OpenFiles.Count) return state;

            var files = view.OpenFiles.ToList();
            files.RemoveAt(action.Index);
            var activeIndex = view.ActiveTabIndex;
            if (files.Count == 0)
                activeIndex = 0;
            else if (activeIndex >= files.Count)
                activeIndex = files.Count - 1;
            else if (activeIndex > action.Index) activeIndex -= 1;

            tabs[action.SolutionId] = view with { OpenFiles = files, ActiveTabIndex = activeIndex };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceCloseAllTabs(
            IdeTabsState state,
            CloseAllTabsAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            var view = tabs.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeTabsViewState.Empty;
            tabs[action.SolutionId] = view with { OpenFiles = new List<IdeOpenFileTab>(), ActiveTabIndex = 0 };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceSetActiveTab(
            IdeTabsState state,
            SetActiveTabAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            var view = tabs.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeTabsViewState.Empty;
            var index = action.Index < 0 ? 0 : action.Index;
            tabs[action.SolutionId] = view with { ActiveTabIndex = index };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceFocusFileTab(
            IdeTabsState state,
            FocusFileTabAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            if (!tabs.TryGetValue(action.SolutionId, out var view)) return state;

            if (action.Index < 0 || action.Index >= view.OpenFiles.Count) return state;

            var files = view.OpenFiles.ToList();
            var file = files[action.Index];
            file.HighlightLine = action.HighlightLine;
            file.HighlightStartLine = action.HighlightStartLine;
            file.HighlightEndLine = action.HighlightEndLine;
            file.RenderLines = IdeTabHelpers.BuildAnnotatedLines(
                file.Content,
                file.DiffLines,
                file.ShowDiff,
                file.HighlightLine,
                file.HighlightStartLine,
                file.HighlightEndLine);
            files[action.Index] = file;
            tabs[action.SolutionId] = view with { OpenFiles = files, ActiveTabIndex = action.Index };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceToggleDiff(
            IdeTabsState state,
            ToggleDiffAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            if (!tabs.TryGetValue(action.SolutionId, out var view) || view.OpenFiles.Count == 0) return state;

            var files = view.OpenFiles.ToList();
            var index = view.ActiveTabIndex;
            if (index < 0 || index >= files.Count) return state;

            var file = files[index];
            file.ShowDiff = action.Enabled;
            file.RenderLines = IdeTabHelpers.BuildAnnotatedLines(
                file.Content,
                file.DiffLines,
                file.ShowDiff,
                file.HighlightLine,
                file.HighlightStartLine,
                file.HighlightEndLine);
            tabs[action.SolutionId] = view with { OpenFiles = files };
            return new IdeTabsState(tabs);
        }

        [ReducerMethod]
        public static IdeTabsState ReduceToggleDiffSuccess(
            IdeTabsState state,
            ToggleDiffSuccessAction action)
        {
            var tabs = new Dictionary<string, IdeTabsViewState>(state.Tabs, StringComparer.OrdinalIgnoreCase);
            if (!tabs.TryGetValue(action.SolutionId, out var view)) return state;

            var files = view.OpenFiles.ToList();
            var index = files.FindIndex(file =>
                string.Equals(file.RepositoryPath, action.Tab.RepositoryPath, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return state;

            files[index] = action.Tab;
            tabs[action.SolutionId] = view with { OpenFiles = files };
            return new IdeTabsState(tabs);
        }
    }
}