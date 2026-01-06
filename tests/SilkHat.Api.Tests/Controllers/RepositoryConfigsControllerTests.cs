using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
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
        var store = new Mock<ILoadedRepositoryStore>();
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny)).Returns(true);
        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new CreateRepositoryConfigRequest(
            "Repo",
            "/tmp/repo",
            null,
            null,
            new List<RepositorySolutionDto> { new("./Repo.sln", true, "solution-1") });

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<RepositoryConfigDto>(created.Value);
        Assert.Equal("Repo", dto.Name);
        Assert.Equal("solution-1", dto.Solutions[0].SolutionId);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
        Assert.Single(dbContext.RepositoryConfigs);
        discovery.Verify(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny), Times.Once);
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

        var store = new Mock<ILoadedRepositoryStore>();
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny)).Returns(true);
        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var request = new UpdateRepositoryConfigRequest(
            "After",
            "/tmp/repo",
            null,
            null,
            new List<RepositorySolutionDto> { new("./Repo.sln", true, "solution-1") });

        var result = await controller.Update(config.Id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RepositoryConfigDto>(ok.Value);
        Assert.Equal("After", dto.Name);
        Assert.Equal("solution-1", dto.Solutions[0].SolutionId);
        Assert.True(interceptor.SaveChangesAsyncCalls > 0);
        discovery.Verify(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_WhenMissing()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var store = new Mock<ILoadedRepositoryStore>();
        var discovery = new Mock<IRepositoryDiscoveryService>();
        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task Create_ValidatesRepository_WhenGroupProvided()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var group = new RepositoryGroup { Id = Guid.NewGuid(), Name = "Group" };
        dbContext.RepositoryGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var store = new Mock<ILoadedRepositoryStore>();
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny)).Returns(false);

        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Create(
            new CreateRepositoryConfigRequest(
                "Repo",
                "/tmp/repo",
                null,
                group.Id,
                new List<RepositorySolutionDto> { new("./Repo.sln", true, "solution-1") }),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        discovery.Verify(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny), Times.Once);
    }

    [Fact]
    public async Task GetLoaded_ReturnsOnlyLoadedConfigs()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var first = new RepositoryConfig { Id = Guid.NewGuid(), Name = "RepoA", RootPath = "/tmp/a" };
        var second = new RepositoryConfig { Id = Guid.NewGuid(), Name = "RepoB", RootPath = "/tmp/b" };
        dbContext.RepositoryConfigs.AddRange(first, second);
        await dbContext.SaveChangesAsync();

        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.GetAll()).Returns(new List<LoadedRepository>
        {
            new(first.Id, "/tmp/a", DateTimeOffset.UtcNow)
        });
        var discovery = new Mock<IRepositoryDiscoveryService>();

        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetLoaded(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var configs = Assert.IsType<List<RepositoryConfigDto>>(ok.Value);
        Assert.Single(configs);
        Assert.Equal(first.Id, configs[0].Id);
        store.Verify(s => s.GetAll(), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsProblem_WhenSolutionsMissing()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var store = new Mock<ILoadedRepositoryStore>();
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.TryValidateRepositoryPath("/tmp/repo", out It.Ref<string?>.IsAny)).Returns(true);
        var controller = new RepositoryConfigsController(dbContext, store.Object, discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Create(
            new CreateRepositoryConfigRequest("Repo", "/tmp/repo", null, null, null),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }
}
