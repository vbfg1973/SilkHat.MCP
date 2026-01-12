namespace SilkHat.Ui.Models
{
    public sealed record CodeTreeAnnotationSelection(CodeTreeAnnotationKind? AnnotationKind);

    public sealed record CodeTreeFilterSelection(
        CodeTreeAnnotationKind? Metric,
        CodeTreeFilterOperator? Operator,
        int? Threshold);
}