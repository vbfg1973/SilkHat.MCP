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
    public sealed class GitCommitsControllerTests
    {
        [Fact]
        public async Task GetCommits_ReturnsProblem_WhenRepositoryMissing()
        {
            var store = new Mock<ILoadedRepositoryStore>();
            var gitCli = new Mock<IGitCli>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((LoadedRepository?)null);
            var controller = new GitCommitsController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetCommits(Guid.NewGuid(), new GitCommitQuery(), CancellationToken.None);

            Assert.IsType<ObjectResult>(result.Result);
            store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetCommits_ReturnsEntries()
        {
            var store = new Mock<ILoadedRepositoryStore>();
            var gitCli = new Mock<IGitCli>();
            var repo = new LoadedRepository(Guid.NewGuid(), "/repo", DateTimeOffset.UtcNow);
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(repo);
            var commits = new List<GitCommitDto>
            {
                new("sha", "sha", Array.Empty<string>(), "Author", "author@example.com", DateTimeOffset.UtcNow,
                    "Subject", "Body", false, new List<GitCommitFileChangeDto>())
            };
            var paged = new PagedResult<GitCommitDto>(commits, 1, 50, commits.Count);
            gitCli.Setup(g => g.QueryCommitsAsync(
                    It.IsAny<Guid>(),
                    repo.RootPath,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);
            var controller = new GitCommitsController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetCommits(repo.ConfigId, new GitCommitQuery(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<PagedResult<GitCommitDto>>(ok.Value);
            Assert.Single(payload.Items);
            gitCli.Verify(g => g.QueryCommitsAsync(
                repo.ConfigId,
                repo.RootPath,
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetCommits_ReturnsValidationProblem_WhenModelInvalid()
        {
            var store = new Mock<ILoadedRepositoryStore>();
            var gitCli = new Mock<IGitCli>();
            var controller = new GitCommitsController(store.Object, gitCli.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };
            controller.ModelState.AddModelError("Sha", "Sha cannot be empty.");

            var result = await controller.GetCommits(Guid.NewGuid(), new GitCommitQuery(), CancellationToken.None);

            Assert.IsType<ObjectResult>(result.Result);
            gitCli.VerifyNoOtherCalls();
        }
    }
}