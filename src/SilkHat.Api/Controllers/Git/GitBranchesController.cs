using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Extensions;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/git/branches")]
public sealed class GitBranchesController : ApiControllerBase
{
    private readonly ILoadedRepositoryStore _store;
    private readonly IGitCli _gitCli;

    public GitBranchesController(ILoadedRepositoryStore store, IGitCli gitCli)
    {
        _store = store;
        _gitCli = gitCli;
    }

    [HttpGet("current")]
    public async Task<ActionResult<string>> GetCurrent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var branch = await _gitCli.GetCurrentBranchAsync(id, repo.RootPath, cancellationToken);
            return Ok(branch);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }

    [HttpGet]
    public async Task<ActionResult<GitBranchListDto>> GetLocalBranches(
        Guid id,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var current = await _gitCli.GetCurrentBranchAsync(id, repo.RootPath, cancellationToken);
            var branches = await _gitCli.ListLocalBranchesAsync(id, repo.RootPath, cancellationToken);
            return Ok(new GitBranchListDto(current, branches));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }
}
