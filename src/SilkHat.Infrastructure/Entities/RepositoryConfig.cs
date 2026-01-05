namespace SilkHat.Infrastructure.Entities;

public sealed class RepositoryConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? GroupId { get; set; }
    public RepositoryGroup? Group { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
