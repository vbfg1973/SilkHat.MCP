namespace SilkHat.Core.Dtos
{
    public sealed record RepositorySolutionDto(
        string RelativePath,
        bool IsEnabled,
        string SolutionId);

    public sealed record AvailableRepositorySolutionDto(
        string RelativePath,
        string SolutionId);
}