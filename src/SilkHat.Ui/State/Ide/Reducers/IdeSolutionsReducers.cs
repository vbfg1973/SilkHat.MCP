using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers;

public static class IdeSolutionsReducers
{
    [ReducerMethod]
    public static IdeSolutionsState ReduceLoadSolutions(
        IdeSolutionsState state,
        LoadSolutionsAction _)
    {
        return new IdeSolutionsState(true, null, state.Solutions, state.SelectedSolutionId);
    }

    [ReducerMethod]
    public static IdeSolutionsState ReduceLoadSolutionsSuccess(
        IdeSolutionsState _,
        LoadSolutionsSuccessAction action)
    {
        return new IdeSolutionsState(false, null, action.Solutions, null);
    }

    [ReducerMethod]
    public static IdeSolutionsState ReduceLoadSolutionsFailure(
        IdeSolutionsState state,
        LoadSolutionsFailureAction action)
    {
        return new IdeSolutionsState(false, action.Error, state.Solutions, state.SelectedSolutionId);
    }

    [ReducerMethod]
    public static IdeSolutionsState ReduceSelectSolution(
        IdeSolutionsState state,
        SelectSolutionAction action)
    {
        return new IdeSolutionsState(state.IsLoading, state.Error, state.Solutions, action.SolutionId);
    }
}
