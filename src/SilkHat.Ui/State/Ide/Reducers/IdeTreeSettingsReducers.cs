using Fluxor;
using SilkHat.Ui.State.Ide.Actions;

namespace SilkHat.Ui.State.Ide.Reducers;

public static class IdeTreeSettingsReducers
{
    [ReducerMethod]
    public static IdeTreeSettingsState ReduceAnnotation(
        IdeTreeSettingsState state,
        SetTreeAnnotationAction action)
    {
        return state with { AnnotationKind = action.AnnotationKind };
    }

    [ReducerMethod]
    public static IdeTreeSettingsState ReduceFilter(
        IdeTreeSettingsState state,
        SetTreeFilterAction action)
    {
        return state with
        {
            FilterMetric = action.FilterMetric,
            FilterOperator = action.FilterOperator,
            FilterThreshold = action.FilterThreshold
        };
    }

    [ReducerMethod]
    public static IdeTreeSettingsState ReduceToggle(
        IdeTreeSettingsState state,
        SetTreeDecorationsEnabledAction action)
    {
        return state with { IsEnabled = action.IsEnabled };
    }
}
