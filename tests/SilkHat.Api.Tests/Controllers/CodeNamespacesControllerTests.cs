using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Models;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class CodeNamespacesControllerTests
    {
        [Fact]
        public void GetNamespaces_ReturnsProblem_WhenWorkspaceMissing()
        {
            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns((CodeRepositoryWorkspace?)null);
            var controller = new CodeNamespacesController(store.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = controller.GetNamespaces(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, null,
                new PagingQuery());

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
            store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public void GetNamespaces_FiltersByPrefix()
        {
            var workspace = CodeWorkspaceFactory.CreateWorkspace();
            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(It.IsAny<Guid>())).Returns(workspace);
            var controller = new CodeNamespacesController(store.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = controller.GetNamespaces(Guid.NewGuid(), CodeWorkspaceFactory.DefaultSolutionId, "Alpha.Sub",
                new PagingQuery());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<PagedResult<string>>(ok.Value);
            Assert.Single(list.Items);
            Assert.Equal("Alpha.Sub", list.Items[0]);
            store.Verify(s => s.Get(It.IsAny<Guid>()), Times.Once);
        }
    }
}