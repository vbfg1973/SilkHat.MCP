using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/git/tree")]
    public sealed class GitTreeController : ApiControllerBase
    {
        private readonly IGitCli _gitCli;
        private readonly ILoadedRepositoryStore _store;

        public GitTreeController(ILoadedRepositoryStore store, IGitCli gitCli)
        {
            _store = store;
            _gitCli = gitCli;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<GitTreeEntryDto>>> GetTree(
            Guid id,
            [FromQuery] string? name,
            [FromQuery] GitTreeEntryType? type,
            [FromQuery] DateTimeOffset? changedAfter,
            [FromQuery] string? author,
            [FromQuery] PagingQuery pagingQuery,
            CancellationToken cancellationToken)
        {
            var repo = _store.Get(id);
            if (repo is null)
                return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded",
                    "Repository is not loaded.", "Git");

            try
            {
                var paging = pagingQuery.ResolvePaging();
                var entries = await _gitCli.ListTreeAsync(
                    id,
                    repo.RootPath,
                    name,
                    type,
                    changedAfter,
                    author,
                    cancellationToken);

                return Ok(entries.ToPagedResult(paging));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Git Error", ex.Message, "Git");
            }
        }
    }
}