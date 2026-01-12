using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Actions
{
    public sealed record SetTreeAnnotationAction(CodeTreeAnnotationKind? AnnotationKind);

    public sealed record SetTreeFilterAction(
        CodeTreeAnnotationKind? FilterMetric,
        CodeTreeFilterOperator? FilterOperator,
        int? FilterThreshold);

    public sealed record SetTreeDecorationsEnabledAction(bool IsEnabled);
}