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
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SilkHatDbContext).Assembly);
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedUtc = now;
                entry.Entity.UpdatedUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedUtc = now;
                entry.Property(entity => entity.CreatedUtc).IsModified = false;
            }
        }
    }
}
