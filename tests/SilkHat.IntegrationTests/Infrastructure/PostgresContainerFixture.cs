using Microsoft.EntityFrameworkCore;
using SilkHat.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace SilkHat.IntegrationTests.Infrastructure;

[CollectionDefinition("Postgres collection")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
}

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("silkhat_tests")
        .WithUsername("silkhat")
        .WithPassword("silkhat")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public SilkHatDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SilkHatDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new SilkHatDbContext(options);
    }
}
