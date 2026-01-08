using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Models;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

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

        var result = controller.GetSolutions(Guid.NewGuid(), new PagingQuery());

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

        var result = controller.GetSolutions(Guid.NewGuid(), new PagingQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<PagedResult<CodeSolutionDto>>(ok.Value);
        Assert.Single(list.Items);
        Assert.Equal(CodeWorkspaceFactory.DefaultSolutionId, list.Items[0].SolutionId);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
