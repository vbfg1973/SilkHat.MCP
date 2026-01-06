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
        discovery.Setup(d => d.ListSolutions("/repos/Repo"))
            .Returns(new List<AvailableRepositorySolutionDto> { new("./Repo.sln", "solution-1") });

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

    [Fact]
    public void GetSolutions_ReturnsSolutionPaths()
    {
        var discovery = new Mock<IRepositoryDiscoveryService>();
        discovery.Setup(d => d.ListSolutions("/repos/Repo")).Returns(new List<AvailableRepositorySolutionDto>
        {
            new("./Repo.sln", "solution-1")
        });

        var controller = new RepositoryDiscoveryController(discovery.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetSolutions("/repos/Repo");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var solutions = Assert.IsType<List<AvailableRepositorySolutionDto>>(ok.Value);
        Assert.Single(solutions);
        Assert.Equal("solution-1", solutions[0].SolutionId);
        discovery.Verify(d => d.ListSolutions("/repos/Repo"), Times.Once);
    }
}
