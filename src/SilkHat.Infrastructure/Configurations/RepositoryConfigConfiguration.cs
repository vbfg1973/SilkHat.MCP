using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure.Configurations
{
    public sealed class RepositoryConfigConfiguration : IEntityTypeConfiguration<RepositoryConfig>
    {
        public void Configure(EntityTypeBuilder<RepositoryConfig> builder)
        {
            builder.HasKey(config => config.Id);
            builder.Property(config => config.Name).IsRequired();
            builder.Property(config => config.RootPath).IsRequired();
            builder.HasOne(config => config.Group)
                .WithMany(group => group.RepositoryConfigs)
                .HasForeignKey(config => config.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(config => config.Solutions)
                .WithOne(solution => solution.RepositoryConfig)
                .HasForeignKey(solution => solution.RepositoryConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}