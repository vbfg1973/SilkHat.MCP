using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure.Configurations;

public sealed class MethodImplementationDecisionConfiguration : IEntityTypeConfiguration<MethodImplementationDecision>
{
    public void Configure(EntityTypeBuilder<MethodImplementationDecision> builder)
    {
        builder.HasKey(decision => decision.Id);
        builder.Property(decision => decision.RepositoryConfigId).IsRequired();
        builder.Property(decision => decision.SolutionId).IsRequired();
        builder.Property(decision => decision.InterfaceTypeName).IsRequired();
        builder.Property(decision => decision.InterfaceMethodSignature).IsRequired();
        builder.Property(decision => decision.ImplementationTypeName).IsRequired();
        builder.HasIndex(decision => new
            {
                decision.RepositoryConfigId,
                decision.SolutionId,
                decision.InterfaceMethodSignature
            })
            .IsUnique();
    }
}
