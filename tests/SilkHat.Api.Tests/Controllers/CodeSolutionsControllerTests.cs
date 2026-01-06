using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeSolutionsControllerTests
{
    [Fact]
    public void GetSolutions_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var controller = new CodeSolutionsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetSolutions(Guid.NewGuid());

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetSolutions_ReturnsSolutions()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(CodeWorkspaceFactory.CreateWorkspace());
        var controller = new CodeSolutionsController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetSolutions(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CodeSolutionDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal(CodeWorkspaceFactory.DefaultSolutionId, list[0].SolutionId);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
