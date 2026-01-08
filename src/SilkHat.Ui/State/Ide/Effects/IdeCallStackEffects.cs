using Fluxor;
using Microsoft.Extensions.Logging;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeCallStackEffects
{
    private readonly RepositoryApiClient _api;
    private readonly IState<IdeSolutionsState> _solutionsState;
    private readonly IState<IdeCallStackState> _callStackState;
    private readonly ILogger<IdeCallStackEffects> _logger;

    public IdeCallStackEffects(
        RepositoryApiClient api,
        IState<IdeSolutionsState> solutionsState,
        IState<IdeCallStackState> callStackState,
        ILogger<IdeCallStackEffects> logger)
    {
        _api = api;
        _solutionsState = solutionsState;
        _callStackState = callStackState;
        _logger = logger;
    }

    [EffectMethod]
    public Task HandleOpenPopup(OpenCallStackPopupAction action, IDispatcher dispatcher)
    {
        var configId = GetConfigId(action.SolutionId);
        if (configId is null)
        {
            dispatcher.Dispatch(new LoadCallStackFailureAction(action.SolutionId, action.SymbolKey, "Solution not found."));
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new LoadCallStackAction(action.SolutionId, configId.Value, action.SymbolKey, null));
        dispatcher.Dispatch(new LoadCallStackMermaidAction(action.SolutionId, configId.Value, action.SymbolKey, null));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleLoadCallStack(LoadCallStackAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: loading call stack for {SymbolKey}", action.SymbolKey);
        try
        {
            var response = await _api.GetMethodCallStackAsync(
                action.ConfigId,
                action.SolutionId,
                new MethodCallStackRequestModel(action.SymbolKey, action.MaxDepth));
            dispatcher.Dispatch(new LoadCallStackSuccessAction(action.SolutionId, action.SymbolKey, response.Nodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to load call stack for {SymbolKey}", action.SymbolKey);
            dispatcher.Dispatch(new LoadCallStackFailureAction(action.SolutionId, action.SymbolKey, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleLoadMermaid(LoadCallStackMermaidAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: loading call stack mermaid for {SymbolKey}", action.SymbolKey);
        try
        {
            var response = await _api.GetMethodCallStackMermaidAsync(
                action.ConfigId,
                action.SolutionId,
                new MethodCallStackRequestModel(action.SymbolKey, action.MaxDepth));
            dispatcher.Dispatch(new LoadCallStackMermaidSuccessAction(action.SolutionId, action.SymbolKey, response.Diagram));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to load call stack mermaid for {SymbolKey}", action.SymbolKey);
            dispatcher.Dispatch(new LoadCallStackMermaidFailureAction(action.SolutionId, action.SymbolKey, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleSaveDecision(SaveCallStackDecisionAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: saving call stack decision for {Signature}", action.InterfaceMethodSignature);
        try
        {
            await _api.SaveMethodImplementationDecisionAsync(
                action.ConfigId,
                action.SolutionId,
                new MethodImplementationDecisionRequestModel(
                    action.InterfaceTypeName,
                    action.InterfaceTypeDocumentationId,
                    action.InterfaceMethodSignature,
                    action.InterfaceMethodDocumentationId,
                    action.ImplementationTypeName,
                    action.ImplementationTypeDocumentationId,
                    action.ImplementationMethodDocumentationId));
            dispatcher.Dispatch(new SaveCallStackDecisionSuccessAction(action.SolutionId, action.InterfaceMethodSignature));

            var view = _callStackState.Value.Views.TryGetValue(action.SolutionId, out var current)
                ? current
                : null;
            if (!string.IsNullOrWhiteSpace(view?.SymbolKey))
            {
                dispatcher.Dispatch(new LoadCallStackAction(action.SolutionId, action.ConfigId, view!.SymbolKey!, null));
                dispatcher.Dispatch(new LoadCallStackMermaidAction(action.SolutionId, action.ConfigId, view!.SymbolKey!, null));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to save call stack decision for {Signature}", action.InterfaceMethodSignature);
            dispatcher.Dispatch(new SaveCallStackDecisionFailureAction(action.SolutionId, action.InterfaceMethodSignature, ex.Message));
        }
    }

    private Guid? GetConfigId(string solutionId)
    {
        var solution = _solutionsState.Value.Solutions
            .FirstOrDefault(item => string.Equals(item.SolutionId, solutionId, StringComparison.OrdinalIgnoreCase));
        return solution?.ConfigId;
    }
}
