using Fluxor;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeTreeEffects
{
    private readonly RepositoryApiClient _api;
    private readonly IState<IdeSolutionsState> _solutionsState;
    private readonly IState<IdeTreeState> _treeState;
    private readonly IState<IdeTreeSettingsState> _settingsState;
    private readonly ILogger<IdeTreeEffects> _logger;

    public IdeTreeEffects(
        RepositoryApiClient api,
        IState<IdeSolutionsState> solutionsState,
        IState<IdeTreeState> treeState,
        IState<IdeTreeSettingsState> settingsState,
        ILogger<IdeTreeEffects> logger)
    {
        _api = api;
        _solutionsState = solutionsState;
        _treeState = treeState;
        _settingsState = settingsState;
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
            var settings = _settingsState.Value;
            var tree = await _api.GetCodeTreeAsync(
                action.ConfigId,
                action.SolutionId,
                parentId: null,
                annotationKind: settings.IsEnabled ? settings.AnnotationKind : null,
                filterMetric: settings.IsEnabled ? settings.FilterMetric : null,
                filterOperator: settings.IsEnabled ? settings.FilterOperator : null,
                filterThreshold: settings.IsEnabled ? settings.FilterThreshold : null);
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
            var settings = _settingsState.Value;
            var children = await _api.GetCodeTreeAsync(
                action.ConfigId,
                action.SolutionId,
                action.ParentId,
                annotationKind: settings.IsEnabled ? settings.AnnotationKind : null,
                filterMetric: settings.IsEnabled ? settings.FilterMetric : null,
                filterOperator: settings.IsEnabled ? settings.FilterOperator : null,
                filterThreshold: settings.IsEnabled ? settings.FilterThreshold : null);
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
            Expandable = entry.Type is CodeTreeEntryType.Project or CodeTreeEntryType.Directory or CodeTreeEntryType.File or CodeTreeEntryType.Type,
            Expanded = false
        };
    }

    private static string GetEntryIcon(CodeTreeEntryModel entry)
    {
        return entry.Type switch
        {
            CodeTreeEntryType.Project => Icons.Material.Filled.AccountTree,
            CodeTreeEntryType.Directory => Icons.Material.Filled.Folder,
            CodeTreeEntryType.Type => Icons.Material.Filled.DataObject,
            CodeTreeEntryType.Member => GetMemberIcon(entry),
            _ => Icons.Material.Filled.Description
        };
    }

    private static string GetMemberIcon(CodeTreeEntryModel entry)
    {
        return entry.RealType?.ToLowerInvariant() switch
        {
            "constructor" => Icons.Material.Filled.Construction,
            "method" => Icons.Material.Filled.Functions,
            "property" => Icons.Material.Filled.Toll,
            "field" => Icons.Material.Filled.Notes,
            "event" => Icons.Material.Filled.Bolt,
            _ => Icons.Material.Filled.Code
        };
    }

    [EffectMethod]
    public Task HandleTreeSettingsChanged(SetTreeAnnotationAction action, IDispatcher dispatcher)
        => ReloadTree(dispatcher);

    [EffectMethod]
    public Task HandleTreeFilterChanged(SetTreeFilterAction action, IDispatcher dispatcher)
        => ReloadTree(dispatcher);

    [EffectMethod]
    public Task HandleTreeToggle(SetTreeDecorationsEnabledAction action, IDispatcher dispatcher)
        => ReloadTree(dispatcher);

    private Task ReloadTree(IDispatcher dispatcher)
    {
        var selected = _solutionsState.Value.SelectedSolutionId;
        if (string.IsNullOrWhiteSpace(selected))
        {
            return Task.CompletedTask;
        }

        var entry = _solutionsState.Value.Solutions
            .FirstOrDefault(solution => string.Equals(solution.SolutionId, selected, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new LoadTreeRootAction(selected, entry.ConfigId));
        return Task.CompletedTask;
    }
}
