using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Models;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeFilesControllerTests
{
    [Fact]
    public async Task GetFile_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var fileService = new Mock<ICodeFileService>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
        var controller = new CodeFilesController(store.Object, fileService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetFile(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, new CodeFileQuery { Path = "Repo/Program.cs" }, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task GetFile_ReturnsOk_WhenFileFound()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var fileService = new Mock<ICodeFileService>();
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var solution = workspace.TryGetSolution(CodeWorkspaceFactory.DefaultSolutionId)!;
        var dto = new CodeFileContentDto("./Program.cs", "Repo/Program.cs", "class Program {}");
        fileService.Setup(s => s.GetFileAsync(workspace, solution, "Repo/Program.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeFileContentResult(CodeFileContentStatus.Success, dto, null));
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
        var controller = new CodeFilesController(store.Object, fileService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetFile(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, new CodeFileQuery { Path = "Repo/Program.cs" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CodeFileContentDto>(ok.Value);
        Assert.Equal("./Program.cs", payload.RepositoryPath);
        fileService.Verify(s => s.GetFileAsync(workspace, solution, "Repo/Program.cs", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFile_ReturnsValidationProblem_WhenModelInvalid()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var fileService = new Mock<ICodeFileService>();
        var controller = new CodeFilesController(store.Object, fileService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };
        controller.ModelState.AddModelError("Path", "Path cannot be empty.");

        var result = await controller.GetFile(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, new CodeFileQuery(), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        fileService.VerifyNoOtherCalls();
    }
}
