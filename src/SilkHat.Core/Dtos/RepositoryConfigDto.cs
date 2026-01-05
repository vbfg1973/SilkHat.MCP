namespace SilkHat.Core.Dtos;

public sealed record RepositoryConfigDto(
    Guid Id,
    string Name,
    string RootPath,
    string? Description,
    Guid? GroupId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);
