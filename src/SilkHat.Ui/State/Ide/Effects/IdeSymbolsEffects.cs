using Fluxor;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Effects
{
    public sealed class IdeSymbolsEffects
    {
        private readonly RepositoryApiClient _api;
        private readonly ILogger<IdeSymbolsEffects> _logger;
        private readonly IState<IdeSolutionsState> _solutionsState;
        private readonly IState<IdeSymbolsState> _symbolsState;
        private readonly IState<IdeTabsState> _tabsState;

        public IdeSymbolsEffects(
            RepositoryApiClient api,
            IState<IdeSolutionsState> solutionsState,
            IState<IdeTabsState> tabsState,
            IState<IdeSymbolsState> symbolsState,
            ILogger<IdeSymbolsEffects> logger)
        {
            _api = api;
            _solutionsState = solutionsState;
            _tabsState = tabsState;
            _symbolsState = symbolsState;
            _logger = logger;
        }

        [EffectMethod]
        public Task HandleOpenPopup(OpenSymbolPopupAction action, IDispatcher dispatcher)
        {
            var activeFile = GetActiveFile(action.SolutionId);
            if (activeFile is null)
            {
                dispatcher.Dispatch(new LoadFileSymbolsFailureAction(action.SolutionId, string.Empty,
                    "No active file tab."));
                return Task.CompletedTask;
            }

            var configId = GetConfigId(action.SolutionId);
            if (configId is null)
            {
                dispatcher.Dispatch(new LoadFileSymbolsFailureAction(action.SolutionId, activeFile.RepositoryPath,
                    "Solution not found."));
                return Task.CompletedTask;
            }

            dispatcher.Dispatch(new LoadFileSymbolsAction(action.SolutionId, configId.Value,
                activeFile.RepositoryPath));
            return Task.CompletedTask;
        }

        [EffectMethod]
        public Task HandleActiveTabChanged(SetActiveTabAction action, IDispatcher dispatcher)
        {
            var view = _symbolsState.Value.Views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            if (!view.IsOpen) return Task.CompletedTask;

            var activeFile = GetActiveFile(action.SolutionId);
            if (activeFile is null) return Task.CompletedTask;

            if (string.Equals(activeFile.RepositoryPath, view.RepositoryPath, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            var configId = GetConfigId(action.SolutionId);
            if (configId is null) return Task.CompletedTask;

            dispatcher.Dispatch(new LoadFileSymbolsAction(action.SolutionId, configId.Value,
                activeFile.RepositoryPath));
            return Task.CompletedTask;
        }

        [EffectMethod]
        public Task HandleOpenFileTabSuccess(OpenFileTabSuccessAction action, IDispatcher dispatcher)
        {
            var view = _symbolsState.Value.Views.TryGetValue(action.SolutionId, out var existing)
                ? existing
                : IdeSymbolsViewState.Empty;
            if (!view.IsOpen) return Task.CompletedTask;

            var configId = GetConfigId(action.SolutionId);
            if (configId is null) return Task.CompletedTask;

            dispatcher.Dispatch(new LoadFileSymbolsAction(action.SolutionId, configId.Value,
                action.Tab.RepositoryPath));
            return Task.CompletedTask;
        }

        [EffectMethod]
        public async Task HandleLoadSymbols(LoadFileSymbolsAction action, IDispatcher dispatcher)
        {
            _logger.LogDebug("IDE: loading symbols for {Path}", action.RepositoryPath);
            try
            {
                var nodes = await _api.GetCodeFileSymbolsAsync(action.ConfigId, action.SolutionId,
                    action.RepositoryPath);
                dispatcher.Dispatch(new LoadFileSymbolsSuccessAction(action.SolutionId, action.RepositoryPath, nodes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IDE: failed to load symbols for {Path}", action.RepositoryPath);
                dispatcher.Dispatch(new LoadFileSymbolsFailureAction(action.SolutionId, action.RepositoryPath,
                    ex.Message));
            }
        }

        private Guid? GetConfigId(string solutionId)
        {
            var solution = _solutionsState.Value.Solutions
                .FirstOrDefault(item => string.Equals(item.SolutionId, solutionId, StringComparison.OrdinalIgnoreCase));
            return solution?.ConfigId;
        }

        private IdeOpenFileTab? GetActiveFile(string solutionId)
        {
            if (!_tabsState.Value.Tabs.TryGetValue(solutionId, out var view)) return null;

            if (view.OpenFiles.Count == 0) return null;

            var index = view.ActiveTabIndex;
            if (index < 0 || index >= view.OpenFiles.Count) return null;

            return view.OpenFiles[index];
        }
    }
}