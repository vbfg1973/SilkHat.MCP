using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Models;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeFileSymbolsControllerTests
{
    [Fact]
    public async Task GetFileSymbols_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var symbols = new Mock<ICodeSymbolOutlineService>();
        var controller = new CodeFileSymbolsController(store.Object, symbols.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetFileSymbols(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new CodeFileQuery { Path = "./Alpha/Foo.cs" },
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task GetFileSymbols_ReturnsOk_WhenSymbolsFound()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);

        var symbols = new Mock<ICodeSymbolOutlineService>();
        symbols.Setup(s => s.GetFileSymbolsAsync(
                workspace,
                It.IsAny<CodeSolutionWorkspace>(),
                "./Alpha/Foo.cs",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeFileSymbolsResult(
                CodeFileSymbolsStatus.Success,
                new List<SymbolOutlineNodeDto>
                {
                    new("sym-1", null, "Foo", "NamedType", "Class", Array.Empty<SymbolOutlineNodeDto>())
                },
                null));

        var controller = new CodeFileSymbolsController(store.Object, symbols.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetFileSymbols(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new CodeFileQuery { Path = "./Alpha/Foo.cs" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<SymbolOutlineNodeDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("Foo", list[0].Name);
    }

    [Fact]
    public async Task GetFileSymbols_ReturnsProblem_WhenSolutionMissing()
    {
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var symbols = new Mock<ICodeSymbolOutlineService>();
        var controller = new CodeFileSymbolsController(store.Object, symbols.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetFileSymbols(
            Guid.NewGuid(),
            "missing-solution",
            new CodeFileQuery { Path = "./Alpha/Foo.cs" },
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}
