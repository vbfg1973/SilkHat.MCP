using Microsoft.EntityFrameworkCore;
using SilkHat.Infrastructure.Entities;
using SilkHat.IntegrationTests.Infrastructure;
using Xunit;

namespace SilkHat.IntegrationTests;

[Collection("Postgres collection")]
public sealed class RepositoryPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;

    public RepositoryPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChanges_PersistsRepositoryConfigAndSolutions()
    {
        await using var dbContext = _fixture.CreateDbContext();
        await ClearDatabaseAsync(dbContext);

        var group = new RepositoryGroup
        {
            Id = Guid.NewGuid(),
            Name = "Group A"
        };

        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = "Repo A",
            RootPath = "/repos/repo-a",
            Group = group,
            Solutions =
            {
                new RepositorySolutionConfig
                {
                    Id = Guid.NewGuid(),
                    RelativePath = "./RepoA.sln",
                    IsEnabled = true
                }
            }
        };

        dbContext.RepositoryGroups.Add(group);
        dbContext.RepositoryConfigs.Add(config);

        await dbContext.SaveChangesAsync();

        var saved = await dbContext.RepositoryConfigs
            .Include(item => item.Solutions)
            .Include(item => item.Group)
            .SingleAsync(item => item.Id == config.Id);

        Assert.Equal("Repo A", saved.Name);
        Assert.Equal("Group A", saved.Group?.Name);
        Assert.Single(saved.Solutions);
        Assert.Equal("./RepoA.sln", saved.Solutions.First().RelativePath);
    }

    private static Task ClearDatabaseAsync(SilkHat.Infrastructure.SilkHatDbContext dbContext)
    {
        return dbContext.Database.ExecuteSqlRawAsync("""
TRUNCATE TABLE "RepositorySolutionConfigs",
               "RepositoryConfigs",
               "RepositoryGroups"
RESTART IDENTITY CASCADE;
""");
    }
}
