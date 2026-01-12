using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Api.Controllers;
using SilkHat.Api.Services;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class RepositoryGroupsControllerTests
    {
        [Fact]
        public async Task Create_PersistsGroup_AndCallsSaveChanges()
        {
            var interceptor = new SaveChangesCounterInterceptor();
            await using var dbContext = DbContextTestFactory.CreateInMemory(interceptor);
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.GetAll()).Returns(new List<LoadedRepository>
            {
                new(Guid.NewGuid(), "/tmp/loaded", DateTimeOffset.UtcNow)
            });
            var codeStore = new Mock<ICodeWorkspaceStore>();
            var gitCache = new Mock<IGitRepositoryCacheStore>();
            var precompute = new Mock<ICodeTreeMetricsPrecomputeService>();
            var processor = new Mock<IRepoCommandProcessor>();
            var loader = new Mock<ICodeWorkspaceLoader>();
            var cache = new FakeApiCache();
            var controller = new RepositoryGroupsController(
                dbContext,
                store.Object,
                codeStore.Object,
                gitCache.Object,
                precompute.Object,
                processor.Object,
                loader.Object,
                cache)
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

            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.GetAll()).Returns(new List<LoadedRepository>
            {
                new(Guid.NewGuid(), "/tmp/loaded", DateTimeOffset.UtcNow)
            });
            var codeStore = new Mock<ICodeWorkspaceStore>();
            var gitCache = new Mock<IGitRepositoryCacheStore>();
            var precompute = new Mock<ICodeTreeMetricsPrecomputeService>();
            var processor = new Mock<IRepoCommandProcessor>();
            var loader = new Mock<ICodeWorkspaceLoader>();
            var cache = new FakeApiCache();
            var controller = new RepositoryGroupsController(
                dbContext,
                store.Object,
                codeStore.Object,
                gitCache.Object,
                precompute.Object,
                processor.Object,
                loader.Object,
                cache)
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
            var store = new Mock<ILoadedRepositoryStore>();
            var codeStore = new Mock<ICodeWorkspaceStore>();
            var gitCache = new Mock<IGitRepositoryCacheStore>();
            var precompute = new Mock<ICodeTreeMetricsPrecomputeService>();
            var processor = new Mock<IRepoCommandProcessor>();
            var loader = new Mock<ICodeWorkspaceLoader>();
            var cache = new FakeApiCache();
            var controller = new RepositoryGroupsController(
                dbContext,
                store.Object,
                codeStore.Object,
                gitCache.Object,
                precompute.Object,
                processor.Object,
                loader.Object,
                cache)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        }

        [Fact]
        public async Task LoadGroup_LoadsAllConfigs()
        {
            await using var dbContext = DbContextTestFactory.CreateInMemory();
            var group = new RepositoryGroup
            {
                Id = Guid.NewGuid(),
                Name = "Group",
                RepositoryConfigs =
                [
                    new RepositoryConfig
                    {
                        Id = Guid.NewGuid(),
                        Name = "Repo A",
                        RootPath = "/tmp/a",
                        Solutions = new List<RepositorySolutionConfig>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(), RelativePath = "./RepoA.sln", SolutionId = "solution-1",
                                IsEnabled = true
                            }
                        }
                    },
                    new RepositoryConfig
                    {
                        Id = Guid.NewGuid(),
                        Name = "Repo B",
                        RootPath = "/tmp/b",
                        Solutions = new List<RepositorySolutionConfig>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(), RelativePath = "./RepoB.sln", SolutionId = "solution-2",
                                IsEnabled = true
                            }
                        }
                    }
                ]
            };
            dbContext.RepositoryGroups.Add(group);
            await dbContext.SaveChangesAsync();

            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.GetAll()).Returns(new List<LoadedRepository>
            {
                new(Guid.NewGuid(), "/tmp/loaded", DateTimeOffset.UtcNow)
            });
            var codeStore = new Mock<ICodeWorkspaceStore>();
            var gitCache = new Mock<IGitRepositoryCacheStore>();
            var precompute = new Mock<ICodeTreeMetricsPrecomputeService>();
            var processor = new Mock<IRepoCommandProcessor>();
            var loader = new Mock<ICodeWorkspaceLoader>();
            processor.Setup(p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .Returns(StreamEvents());

            var cache = new FakeApiCache();
            var controller = new RepositoryGroupsController(
                dbContext,
                store.Object,
                codeStore.Object,
                gitCache.Object,
                precompute.Object,
                processor.Object,
                loader.Object,
                cache)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.LoadGroup(group.Id, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<RepositoryGroupLoadResultDto>(ok.Value);
            Assert.Equal(2, dto.Repositories.Count);
            Assert.Equal(1, cache.InvalidateCount);
            processor.Verify(
                p => p.ExecuteAsync(It.IsAny<IRepoCommand>(), It.IsAny<RepoCommandContext>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));
            store.Verify(s => s.Unload(It.IsAny<Guid>()), Times.AtLeastOnce);
            codeStore.Verify(s => s.Remove(It.IsAny<Guid>()), Times.AtLeastOnce);
            gitCache.Verify(s => s.Remove(It.IsAny<Guid>()), Times.AtLeastOnce);
        }

        private static async IAsyncEnumerable<RepoEventDto> StreamEvents()
        {
            yield return new RepoEventDto(RepoEventKind.Completed, "load", "ok", 100, null, null,
                new RepoEventSummaryDto("Loaded."));
            await Task.CompletedTask;
        }
    }
}