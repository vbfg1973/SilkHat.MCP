using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure.Configurations
{
    public sealed class RepositorySolutionConfigConfiguration : IEntityTypeConfiguration<RepositorySolutionConfig>
    {
        public void Configure(EntityTypeBuilder<RepositorySolutionConfig> builder)
        {
            builder.HasKey(solution => solution.Id);
            builder.Property(solution => solution.RelativePath).IsRequired();
            builder.Property(solution => solution.SolutionId).IsRequired();
        }
    }
}