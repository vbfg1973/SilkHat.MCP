namespace SilkHat.Core.Dtos;

public sealed record CreateRepositoryGroupRequest(
    string Name,
    string? Description);

public sealed record UpdateRepositoryGroupRequest(
    string Name,
    string? Description);
