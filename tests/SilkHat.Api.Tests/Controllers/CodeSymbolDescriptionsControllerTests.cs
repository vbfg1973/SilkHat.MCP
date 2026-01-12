using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class CodeSymbolDescriptionsControllerTests
    {
        private const string solutionId = "solution-1";

        private static readonly Guid workspaceId = Guid.NewGuid();

        [Fact]
        public async Task GetNamedType_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribeNamedTypeAsync(
                    solution,
                    workspace,
                    "T:Sample.Type",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<NamedTypeDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new NamedTypeDescriptionDto(
                        "T:Sample.Type",
                        "Type",
                        "Sample",
                        "Sample.Type",
                        "Class",
                        "Public",
                        false,
                        Array.Empty<string>(),
                        new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false),
                        null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetNamedType(workspaceId, solutionId, "T:Sample.Type", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<NamedTypeDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribeNamedTypeAsync(
                solution,
                workspace,
                "T:Sample.Type",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetMethod_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribeMethodAsync(
                    solution,
                    workspace,
                    "M:Sample.Type.Run",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<MethodDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new MethodDescriptionDto(
                        "M:Sample.Type.Run",
                        "Run",
                        "Type",
                        "Sample",
                        "Sample.Type.Run",
                        "System.Void",
                        "Public",
                        new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false),
                        Array.Empty<SymbolParameterDto>(),
                        Array.Empty<string>(),
                        null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetMethod(workspaceId, solutionId, "M:Sample.Type.Run", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<MethodDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribeMethodAsync(
                solution,
                workspace,
                "M:Sample.Type.Run",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetProperty_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribePropertyAsync(
                    solution,
                    workspace,
                    "P:Sample.Type.Value",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<PropertyDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new PropertyDescriptionDto(
                        "P:Sample.Type.Value",
                        "Value",
                        "Type",
                        "Sample",
                        "Sample.Type.Value",
                        "System.Int32",
                        "Public",
                        true,
                        true,
                        false,
                        new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false),
                        null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetProperty(workspaceId, solutionId, "P:Sample.Type.Value", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<PropertyDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribePropertyAsync(
                solution,
                workspace,
                "P:Sample.Type.Value",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetField_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribeFieldAsync(
                    solution,
                    workspace,
                    "F:Sample.Type.Field",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<FieldDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new FieldDescriptionDto(
                        "F:Sample.Type.Field",
                        "Field",
                        "Type",
                        "Sample",
                        "Sample.Type.Field",
                        "System.String",
                        "Public",
                        new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false),
                        null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result =
                await controller.GetField(workspaceId, solutionId, "F:Sample.Type.Field", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<FieldDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribeFieldAsync(
                solution,
                workspace,
                "F:Sample.Type.Field",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetEvent_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribeEventAsync(
                    solution,
                    workspace,
                    "E:Sample.Type.Changed",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<EventDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new EventDescriptionDto(
                        "E:Sample.Type.Changed",
                        "Changed",
                        "Type",
                        "Sample",
                        "Sample.Type.Changed",
                        "System.EventHandler",
                        "Public",
                        new SymbolModifiersDto(false, false, false, false, false, false, false, false, false, false),
                        null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetEvent(workspaceId, solutionId, "E:Sample.Type.Changed",
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<EventDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribeEventAsync(
                solution,
                workspace,
                "E:Sample.Type.Changed",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetNamespace_ReturnsDescription_AndCallsService()
        {
            var (workspace, solution) = CreateWorkspace();
            var symbolService = new Mock<ISymbolDescriptionService>();
            symbolService.Setup(s => s.DescribeNamespaceAsync(
                    solution,
                    workspace,
                    "N:Sample",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolDescriptionResult<NamespaceDescriptionDto>(
                    SymbolDescriptionStatus.Success,
                    new NamespaceDescriptionDto("N:Sample", "Sample", "Sample", false, null),
                    null));

            var controller = new CodeSymbolDescriptionsController(MockStore(workspace), symbolService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetNamespace(workspaceId, solutionId, "N:Sample", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<NamespaceDescriptionDto>(ok.Value);
            symbolService.Verify(s => s.DescribeNamespaceAsync(
                solution,
                workspace,
                "N:Sample",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetNamedType_ReturnsProblem_WhenRepositoryNotLoaded()
        {
            var symbolService = new Mock<ISymbolDescriptionService>();
            var controller =
                new CodeSymbolDescriptionsController(new Mock<ICodeWorkspaceStore>().Object, symbolService.Object)
                {
                    ControllerContext = ControllerTestFactory.CreateContext()
                };

            var result =
                await controller.GetNamedType(Guid.NewGuid(), solutionId, "T:Sample.Type", CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        }

        private static (CodeRepositoryWorkspace Workspace, CodeSolutionWorkspace Solution) CreateWorkspace()
        {
            var solution = new CodeSolutionWorkspace(
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

            var workspace = new CodeRepositoryWorkspace("/repos/sample", new Dictionary<string, CodeSolutionWorkspace>
            {
                [solutionId] = solution
            });

            return (workspace, solution);
        }

        private static ICodeWorkspaceStore MockStore(CodeRepositoryWorkspace workspace)
        {
            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(workspaceId)).Returns(workspace);
            return store.Object;
        }
    }
}