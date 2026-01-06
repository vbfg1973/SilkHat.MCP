namespace SilkHat.Infrastructure.Entities;

public sealed class RepositorySolutionConfig
{
    public Guid Id { get; set; }
    public Guid RepositoryConfigId { get; set; }
    public RepositoryConfig? RepositoryConfig { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string SolutionId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
