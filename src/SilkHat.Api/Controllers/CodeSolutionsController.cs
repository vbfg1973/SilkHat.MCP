using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions")]
public sealed class CodeSolutionsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;

    public CodeSolutionsController(ICodeWorkspaceStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CodeSolutionDto>> GetSolutions(Guid id)
    {
        var workspace = _store.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var results = workspace.Solutions.Values
            .OrderBy(solution => solution.SolutionName, StringComparer.OrdinalIgnoreCase)
            .Select(solution => new CodeSolutionDto(solution.SolutionId, solution.SolutionName, solution.RelativePath))
            .ToList();

        return Ok(results);
    }
}
