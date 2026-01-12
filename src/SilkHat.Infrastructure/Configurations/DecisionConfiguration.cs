using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure.Configurations
{
    public sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
    {
        public void Configure(EntityTypeBuilder<Decision> builder)
        {
            builder.HasKey(decision => decision.Id);
            builder.Property(decision => decision.SolutionId)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(decision => decision.Name)
                .HasMaxLength(400)
                .IsRequired();
            builder.Property(decision => decision.SubjectKey)
                .HasMaxLength(400)
                .IsRequired();
            builder.Property(decision => decision.PayloadJson)
                .IsRequired();
            builder.Property(decision => decision.Notes)
                .HasMaxLength(4000);
            builder.HasIndex(decision => new
                {
                    decision.RepositoryConfigId,
                    decision.SolutionId,
                    decision.DecisionType,
                    decision.SubjectKey
                })
                .IsUnique();
        }
    }
}