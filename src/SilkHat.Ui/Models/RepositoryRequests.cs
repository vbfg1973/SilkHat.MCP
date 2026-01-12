namespace SilkHat.Ui.Models
{
    public sealed record CreateRepositoryGroupRequest(
        string Name,
        string? Description);

    public sealed record UpdateRepositoryGroupRequest(
        string Name,
        string? Description);

    public sealed record CreateRepositoryConfigRequest(
        string Name,
        string RootPath,
        string? Description,
        Guid? GroupId,
        IReadOnlyList<RepositorySolutionModel>? Solutions);

    public sealed record UpdateRepositoryConfigRequest(
        string Name,
        string RootPath,
        string? Description,
        Guid? GroupId,
        IReadOnlyList<RepositorySolutionModel>? Solutions);
}