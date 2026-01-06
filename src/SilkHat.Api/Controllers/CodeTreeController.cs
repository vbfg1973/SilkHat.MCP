using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/tree")]
public sealed class CodeTreeController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;

    public CodeTreeController(ICodeWorkspaceStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CodeTreeEntryDto>> GetTree(Guid id)
    {
        var workspace = _store.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Code");
        }

        return Ok(workspace.TreeEntries);
    }
}
