using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class GitBranchesControllerTests
{
    [Fact]
    public async Task GetCurrent_ReturnsProblem_WhenRepositoryNotLoaded()
    {
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((LoadedRepository?)null);
        var gitCli = new Mock<IGitCli>();
        var controller = new GitBranchesController(store.Object, gitCli.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetCurrent(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        gitCli.Verify(c => c.GetCurrentBranchAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrent_ReturnsBranchName()
    {
        var configId = Guid.NewGuid();
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
        var gitCli = new Mock<IGitCli>();
        gitCli.Setup(c => c.GetCurrentBranchAsync(configId, "/repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync("feature/demo");

        var controller = new GitBranchesController(store.Object, gitCli.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetCurrent(configId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("feature/demo", ok.Value);
    }

    [Fact]
    public async Task GetLocalBranches_ReturnsBranchList()
    {
        var configId = Guid.NewGuid();
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.Get(configId)).Returns(new LoadedRepository(configId, "/repo", DateTimeOffset.UtcNow));
        var gitCli = new Mock<IGitCli>();
        gitCli.Setup(c => c.GetCurrentBranchAsync(configId, "/repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync("develop");
        gitCli.Setup(c => c.ListLocalBranchesAsync(configId, "/repo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "develop", "feature/demo" });

        var controller = new GitBranchesController(store.Object, gitCli.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetLocalBranches(configId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<GitBranchListDto>(ok.Value);
        Assert.Equal("develop", dto.CurrentBranch);
        Assert.Equal(2, dto.LocalBranches.Count);
    }
}
