using Microsoft.EntityFrameworkCore;
using SilkHat.Infrastructure.Entities;
using SilkHat.IntegrationTests.Infrastructure;
using Xunit;

namespace SilkHat.IntegrationTests;

[Collection("Postgres collection")]
public sealed class MethodImplementationDecisionPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;

    public MethodImplementationDecisionPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChanges_PersistsMethodImplementationDecision()
    {
        await using var dbContext = _fixture.CreateDbContext();
        await ClearDatabaseAsync(dbContext);

        var decision = new MethodImplementationDecision
        {
            Id = Guid.NewGuid(),
            RepositoryConfigId = Guid.NewGuid(),
            SolutionId = "solution-1",
            InterfaceTypeName = "Example.IService",
            InterfaceTypeDocumentationId = "T:Example.IService",
            InterfaceMethodSignature = "Example.IService.DoWork.none",
            InterfaceMethodDocumentationId = "M:Example.IService.DoWork",
            ImplementationTypeName = "Example.Service",
            ImplementationTypeDocumentationId = "T:Example.Service",
            ImplementationMethodDocumentationId = "M:Example.Service.DoWork"
        };

        dbContext.MethodImplementationDecisions.Add(decision);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.MethodImplementationDecisions
            .SingleAsync(item => item.Id == decision.Id);

        Assert.Equal("Example.IService", saved.InterfaceTypeName);
        Assert.Equal("Example.Service", saved.ImplementationTypeName);
        Assert.Equal("T:Example.IService", saved.InterfaceTypeDocumentationId);
        Assert.Equal("M:Example.IService.DoWork", saved.InterfaceMethodDocumentationId);
    }

    private static Task ClearDatabaseAsync(SilkHat.Infrastructure.SilkHatDbContext dbContext)
    {
        return dbContext.Database.ExecuteSqlRawAsync("""
TRUNCATE TABLE "MethodImplementationDecisions"
RESTART IDENTITY CASCADE;
""");
    }
}
