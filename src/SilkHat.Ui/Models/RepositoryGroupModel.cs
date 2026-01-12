namespace SilkHat.Ui.Models
{
    public sealed record RepositoryGroupModel(
        Guid Id,
        string Name,
        string? Description,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc);
}