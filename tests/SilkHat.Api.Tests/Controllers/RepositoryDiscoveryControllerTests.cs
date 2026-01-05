using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class RepositoryDiscoveryControllerTests
{
    [Fact]
    public void GetAvailable_ReturnsRepositories()
    {
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.ListAvailableRepositories()).Returns(new List<AvailableRepositoryDto>
        {
            new("Repo", "Repo", "/repos/Repo", true)
        });

        var controller = new RepositoryDiscoveryController(discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetAvailable();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var repos = Assert.IsType<List<AvailableRepositoryDto>>(ok.Value);
        Assert.Single(repos);
        discovery.Verify(d => d.ListAvailableRepositories(), Times.Once);
    }

    [Fact]
    public void GetAvailable_ReturnsProblem_WhenMissingConfig()
    {
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.ListAvailableRepositories()).Throws(new InvalidOperationException("Missing"));

        var controller = new RepositoryDiscoveryController(discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetAvailable();

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, problem.StatusCode);
    }
}
