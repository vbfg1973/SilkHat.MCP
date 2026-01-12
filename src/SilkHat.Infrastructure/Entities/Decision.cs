using SilkHat.Core.Dtos;

namespace SilkHat.Infrastructure.Entities
{
    public sealed class Decision : BaseEntity
    {
        public Guid RepositoryConfigId { get; set; }
        public string SolutionId { get; set; } = string.Empty;
        public DecisionType DecisionType { get; set; }
        public DecisionStatus Status { get; set; }
        public bool IsActive { get; set; }
        public bool IsValid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubjectKey { get; set; } = string.Empty;
        public DateTimeOffset DiscoveredUtc { get; set; }
        public DateTimeOffset? ResolvedUtc { get; set; }
        public string? Notes { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
    }
}