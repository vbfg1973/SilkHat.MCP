namespace SilkHat.Infrastructure.Entities;

public sealed class RepositoryGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }

    public ICollection<RepositoryConfig> RepositoryConfigs { get; set; } = new List<RepositoryConfig>();
}
