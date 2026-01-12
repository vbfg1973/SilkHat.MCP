using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/code/solutions/{solutionId}/methods")]
    public sealed class CodeMethodsController : ApiControllerBase
    {
        private readonly ICodeWorkspaceStore _codeStore;
        private readonly IMethodComplexityService _methodComplexityService;

        public CodeMethodsController(ICodeWorkspaceStore codeStore, IMethodComplexityService methodComplexityService)
        {
            _codeStore = codeStore;
            _methodComplexityService = methodComplexityService;
        }

        [HttpGet("complexity")]
        public async Task<ActionResult<ComplexityResultDto>> GetMethodComplexity(
            Guid id,
            string solutionId,
            [FromQuery] string? docId,
            [FromQuery] ComplexityMeasureType? measure,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(docId))
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Invalid Request", "docId is required.",
                    "Code");

            if (!measure.HasValue)
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Invalid Request", "measure is required.",
                    "Code");

            var workspace = _codeStore.Get(id);
            if (workspace is null)
                return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded",
                    "Repository code workspace is not loaded.", "Code");

            var solution = workspace.TryGetSolution(solutionId);
            if (solution is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");

            var result = await _methodComplexityService.GetMethodComplexityAsync(
                solution,
                docId,
                measure.Value,
                cancellationToken);

            if (result is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Method not found.", "Code");

            return Ok(result);
        }
    }
}