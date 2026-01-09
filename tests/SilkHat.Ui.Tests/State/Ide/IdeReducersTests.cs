using MudBlazor;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Models;
using SilkHat.Ui.State.Ide.Reducers;

namespace SilkHat.Ui.Tests.State.Ide;

public sealed class IdeReducersTests
{
    [Fact]
    public void SolutionsReducer_LoadsAndSelects()
    {
        var initial = new IdeSolutionsState();
        var loading = IdeSolutionsReducers.ReduceLoadSolutions(initial, new LoadSolutionsAction());
        Assert.True(loading.IsLoading);

        var solutions = new List<IdeSolutionEntry>
        {
            new("solution-1", "./RepoOne.sln", "./RepoOne.sln", "Repo One", Guid.NewGuid(), "./RepoOne.sln (Repo One)")
        };
        var loaded = IdeSolutionsReducers.ReduceLoadSolutionsSuccess(initial, new LoadSolutionsSuccessAction(solutions));
        Assert.False(loaded.IsLoading);
        Assert.Single(loaded.Solutions);

        var selected = IdeSolutionsReducers.ReduceSelectSolution(loaded, new SelectSolutionAction("solution-1"));
        Assert.Equal("solution-1", selected.SelectedSolutionId);
    }

    [Fact]
    public void TreeReducer_LoadsRootAndChildren()
    {
        var state = new IdeTreeState();
        var loading = IdeTreeReducers.ReduceLoadRoot(state, new LoadTreeRootAction("solution-1", Guid.NewGuid()));
        Assert.True(loading.Trees["solution-1"].IsLoading);

        var rootItems = new List<TreeItemData<CodeTreeEntryModel>>
        {
            new()
            {
                Value = new CodeTreeEntryModel("./RepoOne", "Repo One", "Repo One", CodeTreeEntryType.Project, "alpha", "Repo One"),
                Text = "Repo One"
            }
        };
        var loaded = IdeTreeReducers.ReduceLoadRootSuccess(state, new LoadTreeRootSuccessAction("solution-1", rootItems));
        Assert.True(loaded.Trees["solution-1"].RootLoaded);
        Assert.Single(loaded.Trees["solution-1"].Items);

        var childItems = new List<TreeItemData<CodeTreeEntryModel>>
        {
            new()
            {
                Value = new CodeTreeEntryModel("./RepoOne/Program.cs", "Repo One/Program.cs", "Program.cs", CodeTreeEntryType.File, "alpha", "Repo One"),
                Text = "Program.cs"
            }
        };
        var updated = IdeTreeReducers.ReduceLoadChildrenSuccess(
            loaded,
            new LoadTreeChildrenSuccessAction("solution-1", "Repo One", childItems));
        Assert.Single(updated.Trees["solution-1"].Items[0].Children!);
    }

    [Fact]
    public void TabsReducer_TogglesDiff()
    {
        var state = new IdeTabsState(new Dictionary<string, IdeTabsViewState>
        {
            ["solution-1"] = new IdeTabsViewState(new List<IdeOpenFileTab>
            {
                new("./Program.cs", "Repo One/Program.cs", "Program.cs", "line1\nline2")
            }, 0, null)
        });

        var diffLines = new List<GitFileDiffLineModel>
        {
            new(1, GitDiffLineKind.Add, "line1 updated")
        };
        state.Tabs["solution-1"].OpenFiles[0].DiffLines = diffLines;

        var toggled = IdeTabsReducers.ReduceToggleDiff(state, new ToggleDiffAction("solution-1", true));
        Assert.True(toggled.Tabs["solution-1"].OpenFiles[0].ShowDiff);
        Assert.NotEmpty(toggled.Tabs["solution-1"].OpenFiles[0].RenderLines);
    }
}
