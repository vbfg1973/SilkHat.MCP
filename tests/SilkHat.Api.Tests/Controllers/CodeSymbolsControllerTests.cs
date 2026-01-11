using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeSymbolsControllerTests
{
    [Fact]
    public void LookupSymbol_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var graph = new Mock<IGraphQueryService>();
        var controller = new CodeSymbolsController(store.Object, graph.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.LookupSymbol(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new SymbolLookupRequest(null, "sym", "NamedType"));

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void LookupSymbol_ReturnsMatch_WhenFound()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var graph = new Mock<IGraphQueryService>();
        GraphNodeDto? unused = null;
        graph.Setup(g => g.TryGetNodeByKey(It.IsAny<string>(), It.IsAny<string>(), out unused))
            .Returns(false);
        var controller = new CodeSymbolsController(store.Object, graph.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.LookupSymbol(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new SymbolLookupRequest(null, "sym-alpha", "Class"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SymbolLookupResultDto>(ok.Value);
        Assert.True(dto.Found);
        Assert.Equal("Foo", dto.Name);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
