using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Actions;

public sealed record LoadSolutionsAction;

public sealed record LoadSolutionsSuccessAction(IReadOnlyList<IdeSolutionEntry> Solutions);

public sealed record LoadSolutionsFailureAction(string Error);

public sealed record SelectSolutionAction(string SolutionId);
