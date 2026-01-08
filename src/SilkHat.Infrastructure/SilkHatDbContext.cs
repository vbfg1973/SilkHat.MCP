using Microsoft.EntityFrameworkCore;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Infrastructure;

public sealed class SilkHatDbContext : DbContext
{
    public SilkHatDbContext(DbContextOptions<SilkHatDbContext> options)
        : base(options)
    {
    }

    public DbSet<RepositoryConfig> RepositoryConfigs => Set<RepositoryConfig>();
    public DbSet<RepositoryGroup> RepositoryGroups => Set<RepositoryGroup>();
    public DbSet<RepositorySolutionConfig> RepositorySolutionConfigs => Set<RepositorySolutionConfig>();
    public DbSet<MethodImplementationDecision> MethodImplementationDecisions => Set<MethodImplementationDecision>();

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RepositoryGroup>(entity =>
        {
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Name).IsRequired();
        });

        modelBuilder.Entity<RepositoryConfig>(entity =>
        {
            entity.HasKey(config => config.Id);
            entity.Property(config => config.Name).IsRequired();
            entity.Property(config => config.RootPath).IsRequired();
            entity.HasOne(config => config.Group)
                .WithMany(group => group.RepositoryConfigs)
                .HasForeignKey(config => config.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(config => config.Solutions)
                .WithOne(solution => solution.RepositoryConfig)
                .HasForeignKey(solution => solution.RepositoryConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RepositorySolutionConfig>(entity =>
        {
            entity.HasKey(solution => solution.Id);
            entity.Property(solution => solution.RelativePath).IsRequired();
            entity.Property(solution => solution.SolutionId).IsRequired();
        });

        modelBuilder.Entity<MethodImplementationDecision>(entity =>
        {
            entity.HasKey(decision => decision.Id);
            entity.Property(decision => decision.RepositoryConfigId).IsRequired();
            entity.Property(decision => decision.SolutionId).IsRequired();
            entity.Property(decision => decision.InterfaceTypeName).IsRequired();
            entity.Property(decision => decision.InterfaceMethodSignature).IsRequired();
            entity.Property(decision => decision.ImplementationTypeName).IsRequired();
            entity.HasIndex(decision => new
                {
                    decision.RepositoryConfigId,
                    decision.SolutionId,
                    decision.InterfaceMethodSignature
                })
                .IsUnique();
        });
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is RepositoryConfig config)
            {
                if (entry.State == EntityState.Added)
                {
                    config.CreatedUtc = now;
                    config.UpdatedUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    config.UpdatedUtc = now;
                }
            }

            if (entry.Entity is RepositoryGroup group)
            {
                if (entry.State == EntityState.Added)
                {
                    group.CreatedUtc = now;
                    group.UpdatedUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    group.UpdatedUtc = now;
                }
            }

            if (entry.Entity is MethodImplementationDecision decision)
            {
                if (entry.State == EntityState.Added)
                {
                    decision.CreatedUtc = now;
                    decision.UpdatedUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    decision.UpdatedUtc = now;
                }
            }
        }
    }
}
