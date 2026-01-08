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

public sealed class CodeNamedTypesControllerTests
{
    [Fact]
    public void GetNamedTypes_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var controller = new CodeNamedTypesController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetNamedTypes(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            null,
            null,
            null,
            null,
            null,
            new PagingQuery());

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetNamedTypes_FiltersByNamespace()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeNamedTypesController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetNamedTypes(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            null,
            "Alpha.Sub",
            null,
            null,
            null,
            new PagingQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<PagedResult<NamedTypeDto>>(ok.Value);
        Assert.Single(list.Items);
        Assert.Equal("Bar", list.Items[0].Name);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
