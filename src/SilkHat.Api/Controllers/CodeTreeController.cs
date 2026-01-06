using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/tree")]
public sealed class CodeTreeController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;

    public CodeTreeController(ICodeWorkspaceStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CodeTreeEntryDto>> GetTree(Guid id, string solutionId)
    {
        var workspace = _store.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Code");
        }

        var solution = workspace.TryGetSolution(solutionId);
        if (solution is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");
        }

        return Ok(solution.TreeEntries);
    }
}
