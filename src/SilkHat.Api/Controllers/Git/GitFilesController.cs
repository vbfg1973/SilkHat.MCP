using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/git/files")]
public sealed class GitFilesController : ApiControllerBase
{
    private readonly ILoadedRepositoryStore _store;
    private readonly IGitCli _gitCli;

    public GitFilesController(ILoadedRepositoryStore store, IGitCli gitCli)
    {
        _store = store;
        _gitCli = gitCli;
    }

    [HttpGet("{path}/history")]
    public async Task<ActionResult<GitFileHistoryDto>> GetHistory(
        Guid id,
        string path,
        [FromQuery] PagingQuery pagingQuery,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var paging = pagingQuery.ResolvePaging();
            var decodedPath = path.Contains('%') ? Uri.UnescapeDataString(path) : path;
            var history = await _gitCli.FileHistoryAsync(
                id,
                repo.RootPath,
                decodedPath,
                paging.PageNumber,
                paging.PageSize,
                cancellationToken);
            return Ok(history);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }

    [HttpGet("{path}/cochanges")]
    public async Task<ActionResult<GitCoChangeStatsDto>> GetCoChanges(
        Guid id,
        string path,
        [FromQuery] PagingQuery pagingQuery,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var paging = pagingQuery.ResolvePaging();
            var decodedPath = path.Contains('%') ? Uri.UnescapeDataString(path) : path;
            var stats = await _gitCli.CoChangeStatsAsync(
                id,
                repo.RootPath,
                decodedPath,
                paging.PageNumber,
                paging.PageSize,
                cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }

    [HttpGet("{path}/last-change")]
    public async Task<ActionResult<GitFileLastChangeDto>> GetLastChange(
        Guid id,
        string path,
        [FromQuery] bool includeDiff,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var decodedPath = path.Contains('%') ? Uri.UnescapeDataString(path) : path;
            var result = await _gitCli.GetFileLastChangeAsync(
                id,
                repo.RootPath,
                decodedPath,
                includeDiff,
                cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }

    [HttpGet("{path}/change-count")]
    public async Task<ActionResult<GitFileChangeCountDto>> GetChangeCount(
        Guid id,
        string path,
        CancellationToken cancellationToken)
    {
        var repo = _store.Get(id);
        if (repo is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository is not loaded.", "Git");
        }

        try
        {
            var decodedPath = path.Contains('%') ? Uri.UnescapeDataString(path) : path;
            var count = await _gitCli.GetFileChangeCountAsync(
                id,
                repo.RootPath,
                decodedPath,
                cancellationToken);
            return Ok(count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
        }
    }
}
