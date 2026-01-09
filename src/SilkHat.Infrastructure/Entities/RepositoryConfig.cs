namespace SilkHat.Infrastructure.Entities;

public sealed class RepositoryConfig : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? GroupId { get; set; }
    public RepositoryGroup? Group { get; set; }
    public ICollection<RepositorySolutionConfig> Solutions { get; set; } = new List<RepositorySolutionConfig>();
}
