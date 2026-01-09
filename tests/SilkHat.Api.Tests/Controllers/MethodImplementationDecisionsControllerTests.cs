using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Controllers;

public sealed class MethodImplementationDecisionsControllerTests
{
    [Fact]
    public async Task CreateDecision_PersistsDecision_AndCallsSaveChanges()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var controller = new MethodImplementationDecisionsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };
        var repositoryId = Guid.NewGuid();

        var request = new MethodImplementationDecisionRequestDto(
            "SilkHat.Sample.App.IGreetingProvider",
            null,
            "SilkHat.Sample.App.IGreetingProvider.GetGreeting.String",
            null,
            "SilkHat.Sample.App.FriendlyGreetingProvider",
            null,
            null);

        var result = await controller.CreateDecision(repositoryId, "solution-1", request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MethodImplementationDecisionDto>(ok.Value);
        Assert.Equal("SilkHat.Sample.App.FriendlyGreetingProvider", dto.ImplementationTypeName);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
        Assert.Single(dbContext.MethodImplementationDecisions);
    }

    [Fact]
    public async Task CreateDecision_UpdatesExistingDecision()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var repositoryId = Guid.NewGuid();
        var decision = new MethodImplementationDecision
        {
            Id = Guid.NewGuid(),
            RepositoryConfigId = repositoryId,
            SolutionId = "solution-1",
            InterfaceTypeName = "SilkHat.Sample.App.IGreetingProvider",
            InterfaceMethodSignature = "SilkHat.Sample.App.IGreetingProvider.GetGreeting.String",
            ImplementationTypeName = "SilkHat.Sample.App.FriendlyGreetingProvider"
        };
        dbContext.MethodImplementationDecisions.Add(decision);
        await dbContext.SaveChangesAsync();

        var controller = new MethodImplementationDecisionsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new MethodImplementationDecisionRequestDto(
            "SilkHat.Sample.App.IGreetingProvider",
            null,
            "SilkHat.Sample.App.IGreetingProvider.GetGreeting.String",
            null,
            "SilkHat.Sample.App.FormalGreetingProvider",
            null,
            null);

        var result = await controller.CreateDecision(repositoryId, "solution-1", request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MethodImplementationDecisionDto>(ok.Value);
        Assert.Equal("SilkHat.Sample.App.FormalGreetingProvider", dto.ImplementationTypeName);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
    }
}
