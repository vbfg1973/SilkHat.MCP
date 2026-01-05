using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/git/tree")]
public sealed class GitTreeController : ApiControllerBase
{
    private readonly ILoadedRepositoryStore _store;
    private readonly IGitCli _gitCli;

    public GitTreeController(ILoadedRepositoryStore store, IGitCli gitCli)
    {
        _store = store;
        _gitCli = gitCli;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GitTreeEntryDto>>> GetTree(
        Guid id,
        [FromQuery] string? name,
        [FromQuery] GitTreeEntryType? type,
        [FromQuery] DateTimeOffset? changedAfter,
        [FromQuery] string? author,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var entries = await _gitCli.ListTreeAsync(
                id,
                repo.RootPath,
                name,
                type,
                changedAfter,
                author,
                cancellationToken);

            return Ok(entries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }
}
