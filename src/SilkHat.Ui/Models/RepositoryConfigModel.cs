namespace SilkHat.Ui.Models;

public sealed record RepositoryConfigModel(
    Guid Id,
    string Name,
    string RootPath,
    string? Description,
    Guid? GroupId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);
