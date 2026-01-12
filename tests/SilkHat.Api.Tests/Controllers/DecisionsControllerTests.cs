using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Moq;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Controllers
{
    public sealed class DecisionsControllerTests
    {
        [Fact]
        public async Task GetPending_ReturnsDecisions_AndCallsService()
        {
            var repositoryId = Guid.NewGuid();
            var solutionId = "solution-1";
            var workspace = CreateWorkspace(solutionId);
            var decisions = new List<DecisionSummaryDto>
            {
                new(Guid.NewGuid(), DecisionType.ResolveInterface, DecisionStatus.Pending, false, true,
                    "IGreetingProvider",
                    "M:Samples.IGreetingProvider.GetGreeting(System.String)", DateTimeOffset.UtcNow, null, null,
                    new object())
            };

            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(repositoryId)).Returns(workspace);

            var decisionService = new Mock<IDecisionService>();
            decisionService.Setup(service => service.GetPendingAsync(
                    workspace,
                    workspace.Solutions[solutionId],
                    repositoryId,
                    null,
                    null,
                    false,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(decisions);

            var controller = new DecisionsController(store.Object, decisionService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var result = await controller.GetPending(
                repositoryId,
                solutionId,
                null,
                null,
                false,
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsAssignableFrom<IReadOnlyList<DecisionSummaryDto>>(ok.Value);
            Assert.Single(payload);
            decisionService.Verify(service => service.GetPendingAsync(
                workspace,
                workspace.Solutions[solutionId],
                repositoryId,
                null,
                null,
                false,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Resolve_ReturnsProblem_WhenDecisionMissing()
        {
            var repositoryId = Guid.NewGuid();
            var solutionId = "solution-1";
            var workspace = CreateWorkspace(solutionId);
            var payload = new ResolveInterfaceDecisionPayloadDto(
                "Samples.IMarker",
                "T:Samples.IMarker",
                "M:Samples.IMarker.Mark",
                Array.Empty<ResolveInterfaceDecisionCandidateDto>(),
                null,
                null);

            var store = new Mock<ICodeWorkspaceStore>();
            store.Setup(s => s.Get(repositoryId)).Returns(workspace);

            var decisionService = new Mock<IDecisionService>();
            decisionService.Setup(service => service.ResolveAsync(
                    workspace,
                    workspace.Solutions[solutionId],
                    repositoryId,
                    It.IsAny<Guid>(),
                    DecisionType.ResolveInterface,
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DecisionSummaryDto?)null);

            var controller = new DecisionsController(store.Object, decisionService.Object)
            {
                ControllerContext = ControllerTestFactory.CreateContext()
            };

            var request = new DecisionResolveRequestDto(
                Guid.NewGuid(),
                DecisionType.ResolveInterface,
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            var result = await controller.Resolve(repositoryId, solutionId, request, CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(404, problem.StatusCode);
        }

        private static CodeRepositoryWorkspace CreateWorkspace(string solutionId)
        {
            var solution = new CodeSolutionWorkspace(
                solutionId,
                "./RepoOne.sln",
                "./RepoOne.sln",
                "./RepoOne.sln",
                new Dictionary<string, ProjectIndex>(),
                Array.Empty<CodeTreeEntryDto>(),
                new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(),
                Array.Empty<string>(),
                Array.Empty<NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, NamedTypeDto>(),
                new Dictionary<string, Compilation>());

            return new CodeRepositoryWorkspace("/repos/sample", new Dictionary<string, CodeSolutionWorkspace>
            {
                [solutionId] = solution
            });
        }
    }
}