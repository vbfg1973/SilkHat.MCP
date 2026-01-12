namespace SilkHat.Core.Dtos
{
    public sealed record CreateRepositoryConfigRequest(
        string Name,
        string RootPath,
        string? Description,
        Guid? GroupId,
        IReadOnlyList<RepositorySolutionDto>? Solutions);

    public sealed record UpdateRepositoryConfigRequest(
        string Name,
        string RootPath,
        string? Description,
        Guid? GroupId,
        IReadOnlyList<RepositorySolutionDto>? Solutions);
}