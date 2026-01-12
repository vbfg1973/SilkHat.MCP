namespace SilkHat.Core.Dtos
{
    public sealed record RepositoryConfigDto(
        Guid Id,
        string Name,
        string RootPath,
        string? Description,
        Guid? GroupId,
        IReadOnlyList<RepositorySolutionDto> Solutions,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc);
}