namespace SilkHat.Core.Dtos;

public sealed record RepositoryGroupDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);
