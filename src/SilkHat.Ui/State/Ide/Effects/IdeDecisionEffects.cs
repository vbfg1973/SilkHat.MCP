using Fluxor;
using Microsoft.Extensions.Logging;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeDecisionEffects
{
    private readonly RepositoryApiClient _apiClient;
    private readonly IState<IdeSolutionsState> _solutionsState;
    private readonly IState<IdeDecisionsState> _decisionsState;
    private readonly ILogger<IdeDecisionEffects> _logger;

    public IdeDecisionEffects(
        RepositoryApiClient apiClient,
        IState<IdeSolutionsState> solutionsState,
        IState<IdeDecisionsState> decisionsState,
        ILogger<IdeDecisionEffects> logger)
    {
        _apiClient = apiClient;
        _solutionsState = solutionsState;
        _decisionsState = decisionsState;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadPending(LoadPendingDecisionsAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            dispatcher.Dispatch(new LoadPendingDecisionsFailureAction(action.SolutionId, "Solution not found."));
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            _logger.LogDebug("Loading pending decisions for solution {SolutionId}", action.SolutionId);
            var decisions = await _apiClient.GetPendingDecisionsAsync(
                repositoryId,
                solutionId,
                action.FilterType,
                action.SortOption == DecisionSortOption.Type ? "type" : "age",
                action.SortDescending);
            dispatcher.Dispatch(new LoadPendingDecisionsSuccessAction(action.SolutionId, decisions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pending decisions for solution {SolutionId}", action.SolutionId);
            dispatcher.Dispatch(new LoadPendingDecisionsFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleLoadResolved(LoadResolvedDecisionsAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            dispatcher.Dispatch(new LoadResolvedDecisionsFailureAction(action.SolutionId, "Solution not found."));
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            _logger.LogDebug("Loading resolved decisions for solution {SolutionId}", action.SolutionId);
            var decisions = await _apiClient.GetResolvedDecisionsAsync(
                repositoryId,
                solutionId,
                action.FilterType,
                action.ActiveFilter,
                action.SortOption == DecisionSortOption.Type ? "type" : "age",
                action.SortDescending);
            dispatcher.Dispatch(new LoadResolvedDecisionsSuccessAction(action.SolutionId, decisions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resolved decisions for solution {SolutionId}", action.SolutionId);
            dispatcher.Dispatch(new LoadResolvedDecisionsFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleDiscover(DiscoverDecisionsAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            dispatcher.Dispatch(new LoadPendingDecisionsFailureAction(action.SolutionId, "Solution not found."));
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            _logger.LogDebug("Discovering decisions for solution {SolutionId}", action.SolutionId);
            await _apiClient.DiscoverDecisionsAsync(
                repositoryId,
                solutionId,
                new DecisionDiscoverRequestModel(action.FilterType));
            ReloadLists(action.SolutionId, dispatcher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover decisions for solution {SolutionId}", action.SolutionId);
            dispatcher.Dispatch(new LoadPendingDecisionsFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleResolve(ResolveDecisionAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            _logger.LogDebug("Resolving decision {DecisionId}", action.Request.DecisionId);
            await _apiClient.ResolveDecisionAsync(
                repositoryId,
                solutionId,
                action.Request);
            ReloadLists(action.SolutionId, dispatcher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve decision {DecisionId}", action.Request.DecisionId);
        }
    }

    [EffectMethod]
    public async Task HandleNotes(UpdateDecisionNotesAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            await _apiClient.UpdateDecisionNotesAsync(
                repositoryId,
                solutionId,
                action.Request);
            ReloadLists(action.SolutionId, dispatcher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update decision notes {DecisionId}", action.Request.DecisionId);
        }
    }

    [EffectMethod]
    public async Task HandleActivate(SetDecisionActiveAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            await _apiClient.SetDecisionActiveAsync(
                repositoryId,
                solutionId,
                action.Request);
            ReloadLists(action.SolutionId, dispatcher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle decision {DecisionId}", action.Request.DecisionId);
        }
    }

    [EffectMethod]
    public async Task HandleValidate(ValidateDecisionAction action, IDispatcher dispatcher)
    {
        var context = TryGetContext(action.SolutionId);
        if (context is null)
        {
            return;
        }

        var (repositoryId, solutionId) = context.Value;

        try
        {
            await _apiClient.ValidateDecisionAsync(
                repositoryId,
                solutionId,
                action.Request);
            ReloadLists(action.SolutionId, dispatcher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate decision {DecisionId}", action.Request.DecisionId);
        }
    }

    private (Guid RepositoryId, string SolutionId)? TryGetContext(string solutionId)
    {
        var solutions = _solutionsState.Value.Solutions;
        var solution = solutions.FirstOrDefault(entry => string.Equals(entry.SolutionId, solutionId, StringComparison.OrdinalIgnoreCase));
        return solution is null ? null : (solution.ConfigId, solution.SolutionId);
    }

    private void ReloadLists(string solutionId, IDispatcher dispatcher)
    {
        var view = _decisionsState.Value.Views.TryGetValue(solutionId, out var current)
            ? current
            : IdeDecisionsViewState.Default;

        dispatcher.Dispatch(new LoadPendingDecisionsAction(
            solutionId,
            view.FilterType,
            view.SortOption,
            view.SortDescending));

        dispatcher.Dispatch(new LoadResolvedDecisionsAction(
            solutionId,
            view.FilterType,
            view.SortOption,
            view.SortDescending,
            view.ResolvedActiveFilter));
    }
}
