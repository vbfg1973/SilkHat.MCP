using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Controllers;

public sealed class RepositoryConfigsControllerTests
{
    [Fact]
    public async Task Create_PersistsConfig_AndCallsSaveChanges()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var controller = new RepositoryConfigsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new CreateRepositoryConfigRequest("Repo", "/tmp/repo", null, null);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<RepositoryConfigDto>(created.Value);
        Assert.Equal("Repo", dto.Name);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
        Assert.Single(dbContext.RepositoryConfigs);
    }

    [Fact]
    public async Task Update_PersistsChanges_AndCallsSaveChanges()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = "Before",
            RootPath = "/tmp/repo"
        };
        dbContext.RepositoryConfigs.Add(config);
        await dbContext.SaveChangesAsync();

        var controller = new RepositoryConfigsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new UpdateRepositoryConfigRequest("After", "/tmp/repo", null, null);

        var result = await controller.Update(config.Id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RepositoryConfigDto>(ok.Value);
        Assert.Equal("After", dto.Name);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_WhenMissing()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var controller = new RepositoryConfigsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}
