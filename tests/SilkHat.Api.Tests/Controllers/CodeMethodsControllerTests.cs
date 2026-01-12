using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class CodeMethodsControllerTests
    {
        [Fact]
        public async Task GetMethodComplexity_ReturnsBadRequest_WhenDocIdMissing()
        {
            var store = new Mock<ICodeWorkspaceStore>();
            var service = new Mock<IMethodComplexityService>();
            var controller = new CodeMethodsController(store.Object, service.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetMethodComplexity(
                Guid.NewGuid(),
                CodeWorkspaceFactory.DefaultSolutionId,
                null,
                ComplexityMeasureType.Cyclomatic,
                CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        }

        [Fact]
        public async Task GetMethodComplexity_ReturnsNotFound_WhenMethodMissing()
        {
            var workspace = CodeWorkspaceFactory.CreateWorkspace();
            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
            var service = new Mock<IMethodComplexityService>();
            service.Setup(s => s.GetMethodComplexityAsync(
                    workspace.Solutions[CodeWorkspaceFactory.DefaultSolutionId],
                    "doc",
                    ComplexityMeasureType.Cognitive,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ComplexityResultDto?)null);

            var controller = new CodeMethodsController(store.Object, service.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetMethodComplexity(
                Guid.NewGuid(),
                CodeWorkspaceFactory.DefaultSolutionId,
                "doc",
                ComplexityMeasureType.Cognitive,
                CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        }

        [Fact]
        public async Task GetMethodComplexity_ReturnsResult_WhenMethodResolved()
        {
            var workspace = CodeWorkspaceFactory.CreateWorkspace();
            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
            var service = new Mock<IMethodComplexityService>();
            var dto = new ComplexityResultDto("doc", ComplexityMeasureType.Cyclomatic, ComplexityTargetKind.Method,
                null, 7);
            service.Setup(s => s.GetMethodComplexityAsync(
                    workspace.Solutions[CodeWorkspaceFactory.DefaultSolutionId],
                    "doc",
                    ComplexityMeasureType.Cyclomatic,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            var controller = new CodeMethodsController(store.Object, service.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetMethodComplexity(
                Guid.NewGuid(),
                CodeWorkspaceFactory.DefaultSolutionId,
                "doc",
                ComplexityMeasureType.Cyclomatic,
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(dto, ok.Value);
            service.Verify(s => s.GetMethodComplexityAsync(
                workspace.Solutions[CodeWorkspaceFactory.DefaultSolutionId],
                "doc",
                ComplexityMeasureType.Cyclomatic,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}