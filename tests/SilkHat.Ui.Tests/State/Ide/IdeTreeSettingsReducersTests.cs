using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Reducers;

namespace SilkHat.Ui.Tests.State.Ide;

public sealed class IdeTreeSettingsReducersTests
{
    [Fact]
    public void SettingsReducer_UpdatesAnnotation()
    {
        var state = IdeTreeSettingsState.Default;

        var updated = IdeTreeSettingsReducers.ReduceAnnotation(state, new SetTreeAnnotationAction(CodeTreeAnnotationKind.CognitiveComplexity));

        Assert.Equal(CodeTreeAnnotationKind.CognitiveComplexity, updated.AnnotationKind);
    }

    [Fact]
    public void SettingsReducer_UpdatesFilter()
    {
        var state = IdeTreeSettingsState.Default;

        var updated = IdeTreeSettingsReducers.ReduceFilter(
            state,
            new SetTreeFilterAction(CodeTreeAnnotationKind.FileChangeCount, CodeTreeFilterOperator.GreaterThan, 4));

        Assert.Equal(CodeTreeAnnotationKind.FileChangeCount, updated.FilterMetric);
        Assert.Equal(CodeTreeFilterOperator.GreaterThan, updated.FilterOperator);
        Assert.Equal(4, updated.FilterThreshold);
    }

    [Fact]
    public void SettingsReducer_TogglesEnabled()
    {
        var state = IdeTreeSettingsState.Default;

        var updated = IdeTreeSettingsReducers.ReduceToggle(state, new SetTreeDecorationsEnabledAction(true));

        Assert.True(updated.IsEnabled);
    }
}
