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

namespace SilkHat.Api.Tests.Controllers;

public sealed class GitTreeControllerTests
{
    [Fact]
    public async Task GetTree_ReturnsProblem_WhenRepositoryNotLoaded()
    {
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((LoadedRepository?)null);
        var gitCli = new Mock<IGitCli>();
        var controller = new GitTreeController(store.Object, gitCli.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetTree(Guid.NewGuid(), null, null, null, null, new PagingQuery(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        gitCli.Verify(c => c.ListTreeAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<GitTreeEntryType?>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTree_ReturnsEntries()
    {
        var configId = Guid.NewGuid();
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
        var gitCli = new Mock<IGitCli>();
        gitCli.Setup(c => c.ListTreeAsync(
                configId,
                "/repo",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GitTreeEntryDto>
            {
                new("./src/Program.cs", "Program.cs", GitTreeEntryType.File, null)
            });

        var controller = new GitTreeController(store.Object, gitCli.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetTree(configId, null, null, null, null, new PagingQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsType<PagedResult<GitTreeEntryDto>>(ok.Value);
        Assert.Single(entries.Items);
        gitCli.Verify(c => c.ListTreeAsync(
            configId,
            "/repo",
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
