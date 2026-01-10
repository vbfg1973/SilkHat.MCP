using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Actions;

public sealed record OpenFileTabAction(string SolutionId, Guid ConfigId, CodeTreeEntryModel Entry);

public sealed record OpenFileTabByPathAction(
    string SolutionId,
    Guid ConfigId,
    string RepositoryPath,
    int? HighlightLine,
    int? HighlightStartLine,
    int? HighlightEndLine);

public sealed record OpenFileTabSuccessAction(string SolutionId, IdeOpenFileTab Tab);

public sealed record OpenFileTabFailureAction(string SolutionId, string Error);

public sealed record FocusFileTabAction(string SolutionId, int Index, int? HighlightLine, int? HighlightStartLine, int? HighlightEndLine);

public sealed record CloseFileTabAction(string SolutionId, int Index);

public sealed record CloseAllTabsAction(string SolutionId);

public sealed record SetActiveTabAction(string SolutionId, int Index);

public sealed record ToggleDiffAction(string SolutionId, bool Enabled);

public sealed record ToggleDiffSuccessAction(string SolutionId, IdeOpenFileTab Tab);
