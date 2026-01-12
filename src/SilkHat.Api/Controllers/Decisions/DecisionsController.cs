using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/code/solutions/{solutionId}/decisions")]
    public sealed class DecisionsController : ApiControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ICodeWorkspaceStore _codeStore;
        private readonly IDecisionService _decisionService;

        public DecisionsController(ICodeWorkspaceStore codeStore, IDecisionService decisionService)
        {
            _codeStore = codeStore;
            _decisionService = decisionService;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IReadOnlyList<DecisionSummaryDto>>> GetPending(
            Guid id,
            string solutionId,
            [FromQuery] DecisionType? type,
            [FromQuery] string? sort,
            [FromQuery] bool descending,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decisions = await _decisionService.GetPendingAsync(
                workspace!,
                solution!,
                id,
                type,
                sort,
                descending,
                cancellationToken);
            return Ok(decisions);
        }

        [HttpGet("resolved")]
        public async Task<ActionResult<IReadOnlyList<DecisionSummaryDto>>> GetResolved(
            Guid id,
            string solutionId,
            [FromQuery] DecisionType? type,
            [FromQuery] bool? active,
            [FromQuery] string? sort,
            [FromQuery] bool descending,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decisions = await _decisionService.GetResolvedAsync(
                workspace!,
                solution!,
                id,
                type,
                active,
                sort,
                descending,
                cancellationToken);
            return Ok(decisions);
        }

        [HttpPost("discover")]
        public async Task<ActionResult<IReadOnlyList<DecisionSummaryDto>>> Discover(
            Guid id,
            string solutionId,
            [FromBody] DecisionDiscoverRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decisions = await _decisionService.DiscoverPendingAsync(
                workspace!,
                solution!,
                id,
                request.Type,
                cancellationToken);
            return Ok(decisions);
        }

        [HttpPost("resolve")]
        public async Task<ActionResult<DecisionSummaryDto>> Resolve(
            Guid id,
            string solutionId,
            [FromBody] DecisionResolveRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var payload = ParsePayload(request.Type, request.Payload);
            if (payload is null)
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Invalid Request",
                    "Payload is invalid for decision type.", "Validation");

            var decision = await _decisionService.ResolveAsync(
                workspace!,
                solution!,
                id,
                request.DecisionId,
                request.Type,
                payload,
                cancellationToken);

            if (decision is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Decision not found.",
                    "Decisions");

            return Ok(decision);
        }

        [HttpPost("notes")]
        public async Task<ActionResult<DecisionSummaryDto>> UpdateNotes(
            Guid id,
            string solutionId,
            [FromBody] DecisionNotesRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decision = await _decisionService.SetNotesAsync(
                workspace!,
                solution!,
                id,
                request.DecisionId,
                request.Notes,
                cancellationToken);

            if (decision is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Decision not found.",
                    "Decisions");

            return Ok(decision);
        }

        [HttpPost("activate")]
        public async Task<ActionResult<DecisionSummaryDto>> SetActive(
            Guid id,
            string solutionId,
            [FromBody] DecisionActivateRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decision = await _decisionService.SetActiveAsync(
                workspace!,
                solution!,
                id,
                request.DecisionId,
                request.IsActive,
                cancellationToken);

            if (decision is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Decision not found.",
                    "Decisions");

            return Ok(decision);
        }

        [HttpPost("validate")]
        public async Task<ActionResult<DecisionSummaryDto>> ValidateDecision(
            Guid id,
            string solutionId,
            [FromBody] DecisionValidateRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!TryGetWorkspace(id, solutionId, out var workspace, out var solution, out var error)) return error!;

            var decision = await _decisionService.ValidateAsync(
                workspace!,
                solution!,
                id,
                request.DecisionId,
                cancellationToken);

            if (decision is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Decision not found.",
                    "Decisions");

            return Ok(decision);
        }

        private bool TryGetWorkspace(
            Guid repositoryId,
            string solutionId,
            out CodeRepositoryWorkspace? workspace,
            out CodeSolutionWorkspace? solution,
            out ActionResult? error)
        {
            workspace = _codeStore.Get(repositoryId);
            if (workspace is null)
            {
                error = ProblemWithCategory(
                    StatusCodes.Status409Conflict,
                    "Repository Not Loaded",
                    "Repository code workspace is not loaded.",
                    "Code");
                solution = null;
                return false;
            }

            solution = workspace.TryGetSolution(solutionId);
            if (solution is null)
            {
                error = ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");
                return false;
            }

            error = null;
            return true;
        }

        private static object? ParsePayload(DecisionType type, JsonElement payload)
        {
            if (type == DecisionType.ResolveInterface)
                return payload.Deserialize<ResolveInterfaceDecisionPayloadDto>(JsonOptions);

            return null;
        }
    }
}