using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Api.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;
using SilkHat.Api.Services;

namespace SilkHat.Api.Tests.Controllers;

public sealed class CodeTreeControllerTests
{
    [Fact]
    public async Task GetTree_ReturnsProblem_WhenWorkspaceMissing()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var queryService = new Mock<ICodeTreeQueryService>();
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((SilkHat.Code.Analysis.Models.CodeRepositoryWorkspace?)null);
        var controller = new CodeTreeController(store.Object, queryService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetTree(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new CodeTreeQuery(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task GetTree_ReturnsEntries()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var queryService = new Mock<ICodeTreeQueryService>();
        var workspace = CodeWorkspaceFactory.CreateWorkspace();
        var entry = new CodeTreeEntryDto("./Repo.csproj", "Repo", "Repo", CodeTreeEntryType.Project, "alpha", "Alpha", null, null, null, null, null, null);
        var solution = workspace.TryGetSolution(CodeWorkspaceFactory.DefaultSolutionId)! with
        {
            TreeEntries = new List<CodeTreeEntryDto> { entry }
        };
        var updated = new SilkHat.Code.Analysis.Models.CodeRepositoryWorkspace(
            workspace.RootPath,
            new Dictionary<string, SilkHat.Code.Analysis.Models.CodeSolutionWorkspace>
            {
                [solution.SolutionId] = solution
            });
        store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(updated);
        queryService.Setup(s => s.GetTreeAsync(
                It.IsAny<Guid>(),
                updated,
                solution,
                It.IsAny<CodeTreeQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CodeTreeEntryDto>(
                new List<CodeTreeEntryDto> { entry },
                PagingDefaults.PageNumber,
                PagingDefaults.PageSize,
                1));
        var controller = new CodeTreeController(store.Object, queryService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetTree(
            Guid.NewGuid(),
            CodeWorkspaceFactory.DefaultSolutionId,
            new CodeTreeQuery(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsType<PagedResult<CodeTreeEntryDto>>(ok.Value);
        Assert.Single(entries.Items);
        Assert.Equal("Repo", entries.Items[0].Name);
        store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
        queryService.Verify(s => s.GetTreeAsync(
            It.IsAny<Guid>(),
            updated,
            solution,
            It.IsAny<CodeTreeQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
