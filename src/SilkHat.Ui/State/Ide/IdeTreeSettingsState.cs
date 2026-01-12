using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide
{
    public sealed record IdeTreeSettingsState
    {
        public bool IsEnabled { get; init; }
        public CodeTreeAnnotationKind? AnnotationKind { get; init; }
        public CodeTreeAnnotationKind? FilterMetric { get; init; }
        public CodeTreeFilterOperator? FilterOperator { get; init; }
        public int? FilterThreshold { get; init; }

        public static IdeTreeSettingsState Default => new()
        {
            IsEnabled = false,
            AnnotationKind = null,
            FilterMetric = null,
            FilterOperator = null,
            FilterThreshold = null
        };
    }
}