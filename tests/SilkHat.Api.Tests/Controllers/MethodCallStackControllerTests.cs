using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers;

public sealed class MethodCallStackControllerTests
{
    [Fact]
    public async Task GetCallStack_ReturnsNodes_AndCallsService()
    {
        var repositoryId = Guid.NewGuid();
        var solutionId = "solution-1";
        var solution = CreateSolution(solutionId);
        var workspace = new CodeRepositoryWorkspace("/repos/sample", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solutionId] = solution
        });

        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(repositoryId)).Returns(workspace);

        var node = new MethodCallStackNode(
            "0_Test.Sample.Run.none_0",
            0,
            0,
            "Test",
            "Sample",
            "Run",
            Array.Empty<string>(),
            "Test.Sample.Run.none",
            "Test",
            "Helper",
            "DoWork",
            Array.Empty<string>(),
            "Test.Helper.DoWork.none",
            null,
            new MethodCallSite("./Sample.cs", 10, 5, 2, 1, 2, 5),
            new DecisionUsage(Guid.NewGuid(), "InterfaceImplementation"),
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<string?>());

        var callStackService = new Mock<IMethodCallStackService>();
        callStackService.Setup(s => s.BuildCallStackAsync(
                workspace,
                solution,
                repositoryId,
                null,
                "symbol-key",
                null,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MethodCallStackResult(new List<MethodCallStackNode> { node }, false, null));

        var controller = new MethodCallStackController(store.Object, callStackService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetCallStack(
            repositoryId,
            solutionId,
            new MethodCallStackRequestDto(null, "symbol-key", null, false),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MethodCallStackResponseDto>(ok.Value);
        Assert.Single(dto.Nodes);
        Assert.Equal("DoWork", dto.Nodes[0].MethodName);
        Assert.NotNull(dto.Nodes[0].Decision);
        callStackService.Verify(s => s.BuildCallStackAsync(
            workspace,
            solution,
            repositoryId,
            null,
            "symbol-key",
            null,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCallStack_ReturnsProblem_WhenRepositoryNotLoaded()
    {
        var store = new Mock<ICodeWorkspaceStore>();
        var callStackService = new Mock<IMethodCallStackService>();
        var controller = new MethodCallStackController(store.Object, callStackService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetCallStack(
            Guid.NewGuid(),
            "solution-1",
            new MethodCallStackRequestDto(null, "symbol-key", null, false),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task GetCallStackMermaid_ReturnsDiagram()
    {
        var repositoryId = Guid.NewGuid();
        var solutionId = "solution-1";
        var solution = CreateSolution(solutionId);
        var workspace = new CodeRepositoryWorkspace("/repos/sample", new Dictionary<string, CodeSolutionWorkspace>
        {
            [solutionId] = solution
        });

        var store = new Mock<ICodeWorkspaceStore>();
        store.Setup(s => s.Get(repositoryId)).Returns(workspace);

        var node = new MethodCallStackNode(
            "0_Test.Sample.Run.none_0",
            0,
            0,
            "Test",
            "Sample",
            "Run",
            Array.Empty<string>(),
            "Test.Sample.Run.none",
            "Test",
            "Helper",
            "DoWork",
            Array.Empty<string>(),
            "Test.Helper.DoWork.none",
            null,
            new MethodCallSite("./Sample.cs", 10, 5, 2, 1, 2, 5),
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<string?>());

        var callStackService = new Mock<IMethodCallStackService>();
        callStackService.Setup(s => s.BuildCallStackAsync(
                workspace,
                solution,
                repositoryId,
                null,
                "symbol-key",
                null,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MethodCallStackResult(new List<MethodCallStackNode> { node }, false, null));

        var controller = new MethodCallStackController(store.Object, callStackService.Object)
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = await controller.GetCallStackMermaid(
            repositoryId,
            solutionId,
            new MethodCallStackRequestDto(null, "symbol-key", null, false),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MethodCallStackMermaidDto>(ok.Value);
        Assert.Contains("sequenceDiagram", dto.Diagram);
        callStackService.Verify(s => s.BuildCallStackAsync(
            workspace,
            solution,
            repositoryId,
            null,
            "symbol-key",
            null,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CodeSolutionWorkspace CreateSolution(string solutionId)
    {
        return new CodeSolutionWorkspace(
            solutionId,
            "Solution",
            "/repos/sample/Solution.sln",
            "./Solution.sln",
            new Dictionary<string, ProjectIndex>(),
            Array.Empty<CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(),
            Array.Empty<string>(),
            Array.Empty<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Compilation>());
    }
}
