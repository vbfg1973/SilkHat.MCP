namespace SilkHat.Ui.State.Ide;

public sealed record IdeSolutionEntry(
    string SolutionId,
    string RelativePath,
    string Name,
    Guid ConfigId,
    string DisplayName);
