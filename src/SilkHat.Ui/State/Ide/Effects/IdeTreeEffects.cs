using Fluxor;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeTreeEffects
{
    private readonly RepositoryApiClient _api;
    private readonly IState<IdeSolutionsState> _solutionsState;
    private readonly IState<IdeTreeState> _treeState;
    private readonly ILogger<IdeTreeEffects> _logger;

    public IdeTreeEffects(
        RepositoryApiClient api,
        IState<IdeSolutionsState> solutionsState,
        IState<IdeTreeState> treeState,
        ILogger<IdeTreeEffects> logger)
    {
        _api = api;
        _solutionsState = solutionsState;
        _treeState = treeState;
        _logger = logger;
    }

    [EffectMethod]
    public Task HandleSelectSolution(SelectSolutionAction action, IDispatcher dispatcher)
    {
        var treeState = _treeState.Value;
        if (treeState.Trees.TryGetValue(action.SolutionId, out var existing) && existing.RootLoaded)
        {
            return Task.CompletedTask;
        }

        var entry = _solutionsState.Value.Solutions
            .FirstOrDefault(solution => string.Equals(solution.SolutionId, action.SolutionId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new LoadTreeRootAction(action.SolutionId, entry.ConfigId));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleLoadTreeRoot(LoadTreeRootAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: loading tree root for solution {SolutionId}", action.SolutionId);
        try
        {
            var tree = await _api.GetCodeTreeAsync(action.ConfigId, action.SolutionId);
            var items = tree.Items.Select(BuildTreeItem).ToList();
            dispatcher.Dispatch(new LoadTreeRootSuccessAction(action.SolutionId, items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to load tree root for solution {SolutionId}", action.SolutionId);
            dispatcher.Dispatch(new LoadTreeRootFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleLoadTreeChildren(LoadTreeChildrenAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: loading tree children for {ParentId}", action.ParentId);
        try
        {
            var children = await _api.GetCodeTreeAsync(action.ConfigId, action.SolutionId, action.ParentId);
            var items = children.Items.Select(BuildTreeItem).ToList();
            dispatcher.Dispatch(new LoadTreeChildrenSuccessAction(action.SolutionId, action.ParentId, items));
            action.Completion.TrySetResult(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to load tree children for {ParentId}", action.ParentId);
            dispatcher.Dispatch(new LoadTreeChildrenFailureAction(action.SolutionId, action.ParentId, ex.Message));
            action.Completion.TrySetException(ex);
        }
    }

    private static TreeItemData<CodeTreeEntryModel> BuildTreeItem(CodeTreeEntryModel entry)
    {
        return new TreeItemData<CodeTreeEntryModel>
        {
            Value = entry,
            Text = entry.Name,
            Icon = GetEntryIcon(entry),
            Expandable = entry.Type == CodeTreeEntryType.Project || entry.Type == CodeTreeEntryType.Directory,
            Expanded = false
        };
    }

    private static string GetEntryIcon(CodeTreeEntryModel entry)
    {
        return entry.Type switch
        {
            CodeTreeEntryType.Project => Icons.Material.Filled.AccountTree,
            CodeTreeEntryType.Directory => Icons.Material.Filled.Folder,
            _ => Icons.Material.Filled.Description
        };
    }
}
