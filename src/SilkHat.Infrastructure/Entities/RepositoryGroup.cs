namespace SilkHat.Infrastructure.Entities
{
    public sealed class RepositoryGroup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<RepositoryConfig> RepositoryConfigs { get; set; } = new List<RepositoryConfig>();
    }
}