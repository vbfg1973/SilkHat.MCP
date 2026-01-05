using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Controllers;

public sealed class RepositoryGroupsControllerTests
{
    [Fact]
    public async Task Create_PersistsGroup_AndCallsSaveChanges()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var controller = new RepositoryGroupsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new CreateRepositoryGroupRequest("Group", null);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<RepositoryGroupDto>(created.Value);
        Assert.Equal("Group", dto.Name);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
        Assert.Single(dbContext.RepositoryGroups);
    }

    [Fact]
    public async Task Update_PersistsChanges_AndCallsSaveChanges()
    {
        var interceptor = new SaveChangesCounterInterceptor();
        await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
        var group = new RepositoryGroup
        {
            Id = Guid.NewGuid(),
            Name = "Before"
        };
        dbContext.RepositoryGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var controller = new RepositoryGroupsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new UpdateRepositoryGroupRequest("After", null);

        var result = await controller.Update(group.Id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RepositoryGroupDto>(ok.Value);
        Assert.Equal("After", dto.Name);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_WhenMissing()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var controller = new RepositoryGroupsController(dbContext)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}
