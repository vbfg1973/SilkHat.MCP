using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/code/solutions/{solutionId}/index/status")]
    public sealed class CodeIndexStatusController : ApiControllerBase
    {
        private readonly IIndexingStatusStore _statusStore;
        private readonly ICodeWorkspaceStore _store;

        public CodeIndexStatusController(ICodeWorkspaceStore store, IIndexingStatusStore statusStore)
        {
            _store = store;
            _statusStore = statusStore;
        }

        [HttpGet]
        public ActionResult<IReadOnlyCollection<IndexJobStatus>> GetStatus(Guid id, string solutionId)
        {
            var workspace = _store.Get(id);
            if (workspace is null)
                return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded",
                    "Repository is not loaded.", "Code");

            var solution = workspace.TryGetSolution(solutionId);
            if (solution is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");

            var status = _statusStore.GetStatus(solution.SolutionId);
            return Ok(status);
        }
    }
}