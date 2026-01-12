using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Api.Controllers;
using SilkHat.Api.Models;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class GitFilesControllerTests
    {
        [Fact]
        public async Task GetHistory_ReturnsProblem_WhenRepositoryNotLoaded()
        {
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((LoadedRepository?)null);
            var gitCli = new Mock<IGitCli>();
            var controller = new GitFilesController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetHistory(Guid.NewGuid(), "src/Program.cs", new PagingQuery(),
                CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
            gitCli.Verify(c => c.FileHistoryAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetHistory_ReturnsEntries()
        {
            var configId = Guid.NewGuid();
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
            var gitCli = new Mock<IGitCli>();
            gitCli.Setup(c => c.FileHistoryAsync(
                    configId,
                    "/repo",
                    "src/Program.cs",
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitFileHistoryDto(
                    "./src/Program.cs",
                    new PagedResult<GitFileHistoryEntryDto>(
                        new List<GitFileHistoryEntryDto>
                        {
                            new("sha", "author", DateTimeOffset.UtcNow, "message", new List<GitFileChangeDto>())
                        },
                        1,
                        50,
                        1)));

            var controller = new GitFilesController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetHistory(configId, "src/Program.cs", new PagingQuery(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<GitFileHistoryDto>(ok.Value);
            Assert.Single(history.Entries.Items);
        }

        [Fact]
        public async Task GetCoChanges_ReturnsEntries()
        {
            var configId = Guid.NewGuid();
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
            var gitCli = new Mock<IGitCli>();
            gitCli.Setup(c => c.CoChangeStatsAsync(
                    configId,
                    "/repo",
                    "src/Program.cs",
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitCoChangeStatsDto(
                    "./src/Program.cs",
                    1,
                    new PagedResult<GitCoChangeEntryDto>(
                        new List<GitCoChangeEntryDto>
                        {
                            new("./src/Other.cs", 2)
                        },
                        1,
                        50,
                        1)));

            var controller = new GitFilesController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetCoChanges(configId, "src/Program.cs", new PagingQuery(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<GitCoChangeStatsDto>(ok.Value);
            Assert.Single(stats.Entries.Items);
        }

        [Fact]
        public async Task GetLastChange_ReturnsCommitMetadata()
        {
            var configId = Guid.NewGuid();
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
            var gitCli = new Mock<IGitCli>();
            gitCli.Setup(c =>
                    c.GetFileLastChangeAsync(configId, "/repo", "src/Program.cs", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitFileLastChangeDto(
                    "./src/Program.cs",
                    "sha1",
                    "sha1",
                    "alice",
                    "alice@example.com",
                    DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
                    "Update",
                    new List<GitFileDiffLineDto>()));

            var controller = new GitFilesController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetLastChange(configId, "src/Program.cs", true, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var change = Assert.IsType<GitFileLastChangeDto>(ok.Value);
            Assert.Equal("sha1", change.CommitSha);
            gitCli.Verify(
                c => c.GetFileLastChangeAsync(configId, "/repo", "src/Program.cs", true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetChangeCount_ReturnsCount()
        {
            var configId = Guid.NewGuid();
            var store = new Mock<ILoadedRepositoryStore>();
            store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
            var gitCli = new Mock<IGitCli>();
            gitCli.Setup(c =>
                    c.GetFileChangeCountAsync(configId, "/repo", "src/Program.cs", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitFileChangeCountDto("./src/Program.cs", 3));

            var controller = new GitFilesController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetChangeCount(configId, "src/Program.cs", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var count = Assert.IsType<GitFileChangeCountDto>(ok.Value);
            Assert.Equal(3, count.ChangeCount);
        }
    }
}