using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;
using SilkHat.Api.Models;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/tree")]
public sealed class CodeTreeController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;
    private readonly ICodeTreeService _treeService;

    public CodeTreeController(ICodeWorkspaceStore store, ICodeTreeService treeService)
    {
        _store = store;
        _treeService = treeService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CodeTreeEntryDto>> GetTree(
        Guid id,
        string solutionId,
        [FromQuery] CodeTreeQuery query)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();
            var message = errors.Count > 0 ? string.Join(" ", errors) : "Validation failed.";
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", message, "Validation");
        }

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

        return Ok(_treeService.GetTree(solution, query.ParentId));
    }
}
