using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/git/commits")]
public sealed class GitCommitsController : ApiControllerBase
{
    private readonly ILoadedRepositoryStore _store;
    private readonly IGitCli _gitCli;

    public GitCommitsController(ILoadedRepositoryStore store, IGitCli gitCli)
    {
        _store = store;
        _gitCli = gitCli;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GitCommitDto>>> GetCommits(
        Guid id,
        [FromQuery] GitCommitQuery query,
        CancellationToken cancellationToken)
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

        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var paging = new PagingQuery { PageNumber = query.PageNumber, PageSize = query.PageSize }.ResolvePaging();
            var commits = await _gitCli.QueryCommitsAsync(
                id,
                repo.RootPath,
                query.Author,
                query.Sha,
                query.Since,
                query.Until,
                query.Merge,
                query.Path,
                paging.PageNumber,
                paging.PageSize,
                cancellationToken);

            return Ok(commits);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }
}
