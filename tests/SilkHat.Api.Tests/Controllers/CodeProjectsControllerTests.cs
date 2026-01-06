using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeProjectsControllerTests
{
    [Fact]
    public void GetProjects_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var controller = new CodeProjectsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetProjects(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, null);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetProjects_FiltersByName()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeProjectsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetProjects(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, "Alpha");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CodeProjectDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("Alpha", list[0].Name);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetProject_ReturnsNotFound_WhenMissing()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeProjectsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetProject(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, "missing");

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetProjectReferences_ReturnsReferences()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeProjectsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetProjectReferences(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, "alpha");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CodeProjectReferenceDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("beta", list[0].ProjectKey);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetProjectReferencedBy_ReturnsReferencedBy()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeProjectsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetProjectReferencedBy(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, "beta");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CodeProjectReferenceDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("alpha", list[0].ProjectKey);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
