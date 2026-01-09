namespace SilkHat.Ui.State.Ide.Actions;

public sealed record SetIdeMainViewAction(string SolutionId, IdeMainView View);

public sealed record SetToolboxVisibilityAction(string SolutionId, bool IsHidden);
