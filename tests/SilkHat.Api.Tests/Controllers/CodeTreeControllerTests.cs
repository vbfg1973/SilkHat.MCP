using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeTreeControllerTests
{
    [Fact]
    public void GetTree_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((SilkHat.Code.Analysis.Models.CodeRepositoryWorkspace?)null);
        var controller = new CodeTreeController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetTree(Guid.NewGuid());

        Assert.IsType<ObjectResult>(result.Result);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public void GetTree_ReturnsEntries()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var entry = new CodeTreeEntryDto("./Repo.csproj", "Repo", "Repo", CodeTreeEntryType.Project, "alpha", "Alpha");
        var updated = new SilkHat.Code.Analysis.Models.CodeRepositoryWorkspace(
            workspace.RootPath,
            workspace.Workspaces,
            workspace.Projects,
            new List<CodeTreeEntryDto> { entry },
            workspace.Namespaces,
            workspace.NamedTypes,
            workspace.NamedTypesBySymbolKey,
            workspace.Compilations);
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(updated);
        var controller = new CodeTreeController(store.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.GetTree(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IReadOnlyList<CodeTreeEntryDto>>(ok.Value);
        Assert.Single(entries);
        Assert.Equal("Repo", entries[0].Name);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }
}
