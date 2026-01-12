namespace SilkHat.Ui.State.Ide
{
    public sealed record IdeSolutionEntry(
        string SolutionId,
        string RelativePath,
        string Name,
        string RepositoryName,
        Guid ConfigId,
        string DisplayName);
}