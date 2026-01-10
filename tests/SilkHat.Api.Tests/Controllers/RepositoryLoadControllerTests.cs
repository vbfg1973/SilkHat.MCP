using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Controllers;

public sealed class RepositoryLoadControllerTests
{
    [Fact]
    public async Task Load_ReturnsProblem_WhenConfigMissing()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var store = new Mock<ILoadedRepositoryStore>();
        var codeStore = new Mock<ICodeWorkspaceStore>();
        var processor = new Mock<IRepoCommandProcessor>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var gitCache = new Mock<IGitRepositoryCacheStore>();

        var cache = new FakeApiCache();
        var controller = new RepositoryLoadController(dbContext, store.Object, codeStore.Object, gitCache.Object, processor.Object, loader.Object, cache)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Load(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal(1, cache.InvalidateCount);
        processor.Verify(p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Load_StreamsEvents_AndCallsProcessor()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = "Repo",
            RootPath = "/tmp/repo",
            Solutions = new List<RepositorySolutionConfig>
            {
                new() { Id = Guid.NewGuid(), RelativePath = "./Repo.sln", SolutionId = "solution-1", IsEnabled = true }
            }
        };
        dbContext.RepositoryConfigs.Add(config);
        await dbContext.SaveChangesAsync();

        var store = new Mock<ILoadedRepositoryStore>();
        var codeStore = new Mock<ICodeWorkspaceStore>();
        var processor = new Mock<IRepoCommandProcessor>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var gitCache = new Mock<IGitRepositoryCacheStore>();

        processor.Setup(p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(), It.IsAny<CancellationToken>()))
            .Returns(StreamEvents());

        var cache = new FakeApiCache();
        var controller = new RepositoryLoadController(dbContext, store.Object, codeStore.Object, gitCache.Object, processor.Object, loader.Object, cache)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Load(config.Id, CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(1, cache.InvalidateCount);
        processor.Verify(p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Load_ReturnsProblem_WhenNoEnabledSolutions()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = "Repo",
            RootPath = "/tmp/repo",
            Solutions = new List<RepositorySolutionConfig>
            {
                new() { Id = Guid.NewGuid(), RelativePath = "./Repo.sln", SolutionId = "solution-1", IsEnabled = false }
            }
        };
        dbContext.RepositoryConfigs.Add(config);
        await dbContext.SaveChangesAsync();

        var store = new Mock<ILoadedRepositoryStore>();
        var codeStore = new Mock<ICodeWorkspaceStore>();
        var processor = new Mock<IRepoCommandProcessor>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var gitCache = new Mock<IGitRepositoryCacheStore>();

        var cache = new FakeApiCache();
        var controller = new RepositoryLoadController(dbContext, store.Object, codeStore.Object, gitCache.Object, processor.Object, loader.Object, cache)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Load(config.Id, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        processor.Verify(p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unload_CallsStores_WhenConfigExists()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = "Repo",
            RootPath = "/tmp/repo"
        };
        dbContext.RepositoryConfigs.Add(config);
        await dbContext.SaveChangesAsync();

        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Unload(config.Id)).Returns(true);
        var codeStore = new Mock<ICodeWorkspaceStore>();
        var processor = new Mock<IRepoCommandProcessor>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var gitCache = new Mock<IGitRepositoryCacheStore>();

        var cache = new FakeApiCache();
        var controller = new RepositoryLoadController(dbContext, store.Object, codeStore.Object, gitCache.Object, processor.Object, loader.Object, cache)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.Unload(config.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RepoEventDto>(ok.Value);
        Assert.Equal(RepoEventKind.Completed, dto.Kind);
        store.Verify(s => s.Unload(config.Id), Times.Once);
        codeStore.Verify(s => s.Remove(config.Id), Times.Once);
        gitCache.Verify(s => s.Remove(config.Id), Times.Once);
    }

    private static async IAsyncEnumerable<RepoEventDto> StreamEvents()
    {
        yield return new RepoEventDto(RepoEventKind.Progress, "stage", "msg", 10, null, null, null);
        await Task.CompletedTask;
    }
}
