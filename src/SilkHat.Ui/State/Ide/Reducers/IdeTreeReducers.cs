using Fluxor;
using MudBlazor;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers;

public static class IdeTreeReducers
{
    [ReducerMethod]
    public static IdeTreeState ReduceLoadRoot(
        IdeTreeState state,
        LoadTreeRootAction action)
    {
        var trees = new Dictionary<string, IdeTreeViewState>(state.Trees, StringComparer.OrdinalIgnoreCase);
        var existing = trees.TryGetValue(action.SolutionId, out var current)
            ? current
            : IdeTreeViewState.Empty;
        trees[action.SolutionId] = existing with { IsLoading = true, Error = null };

        return new IdeTreeState(trees);
    }

    [ReducerMethod]
    public static IdeTreeState ReduceLoadRootSuccess(
        IdeTreeState state,
        LoadTreeRootSuccessAction action)
    {
        var trees = new Dictionary<string, IdeTreeViewState>(state.Trees, StringComparer.OrdinalIgnoreCase)
        {
            [action.SolutionId] = new IdeTreeViewState(false, true, null, action.Items.ToList())
        };

        return new IdeTreeState(trees);
    }

    [ReducerMethod]
    public static IdeTreeState ReduceLoadRootFailure(
        IdeTreeState state,
        LoadTreeRootFailureAction action)
    {
        var trees = new Dictionary<string, IdeTreeViewState>(state.Trees, StringComparer.OrdinalIgnoreCase);
        var existing = trees.TryGetValue(action.SolutionId, out var current)
            ? current
            : IdeTreeViewState.Empty;
        trees[action.SolutionId] = existing with { IsLoading = false, Error = action.Error };

        return new IdeTreeState(trees);
    }

    [ReducerMethod]
    public static IdeTreeState ReduceLoadChildrenSuccess(
        IdeTreeState state,
        LoadTreeChildrenSuccessAction action)
    {
        var trees = new Dictionary<string, IdeTreeViewState>(state.Trees, StringComparer.OrdinalIgnoreCase);
        if (!trees.TryGetValue(action.SolutionId, out var tree))
        {
            return state;
        }

        var items = tree.Items.ToList();
        if (TryUpdateChildren(items, action.ParentId, action.Items))
        {
            trees[action.SolutionId] = tree with { Items = items };
            return new IdeTreeState(trees);
        }

        return state;
    }

    [ReducerMethod]
    public static IdeTreeState ReduceLoadChildrenFailure(
        IdeTreeState state,
        LoadTreeChildrenFailureAction action)
    {
        var trees = new Dictionary<string, IdeTreeViewState>(state.Trees, StringComparer.OrdinalIgnoreCase);
        if (!trees.TryGetValue(action.SolutionId, out var tree))
        {
            return state;
        }

        trees[action.SolutionId] = tree with { Error = action.Error };
        return new IdeTreeState(trees);
    }

    private static bool TryUpdateChildren(
        IEnumerable<TreeItemData<CodeTreeEntryModel>> items,
        string parentId,
        IReadOnlyList<TreeItemData<CodeTreeEntryModel>> children)
    {
        foreach (var item in items)
        {
            if (item.Value is not null && string.Equals(item.Value.DisplayPath, parentId, StringComparison.OrdinalIgnoreCase))
            {
                item.Children = children.ToList();
                return true;
            }

            if (item.Children is not null && item.Children.Count > 0)
            {
                if (TryUpdateChildren(item.Children, parentId, children))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
