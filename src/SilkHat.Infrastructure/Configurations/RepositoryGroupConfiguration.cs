using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure.Configurations
{
    public sealed class RepositoryGroupConfiguration : IEntityTypeConfiguration<RepositoryGroup>
    {
        public void Configure(EntityTypeBuilder<RepositoryGroup> builder)
        {
            builder.HasKey(group => group.Id);
            builder.Property(group => group.Name).IsRequired();
        }
    }
}