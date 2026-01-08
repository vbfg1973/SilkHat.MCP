using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/available")]
public sealed class RepositoryDiscoveryController : ApiControllerBase
{
    private readonly IRepositoryDiscoveryService _discovery;

    public RepositoryDiscoveryController(IRepositoryDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    [HttpGet]
    public ActionResult<PagedResult<AvailableRepositoryDto>> GetAvailable([FromQuery] PagingQuery pagingQuery)
    {
        try
        {
            var paging = pagingQuery.ResolvePaging();
            var results = _discovery.ListAvailableRepositories();
            return Ok(results.ToPagedResult(paging));
        }
        catch (InvalidOperationException ex)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Configuration Error", ex.Message, "Configuration");
        }
    }

    [HttpGet("solutions")]
    public ActionResult<PagedResult<AvailableRepositorySolutionDto>> GetSolutions(
        [FromQuery] string path,
        [FromQuery] PagingQuery pagingQuery)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "Path is required.", "Validation");
        }

        var decodedPath = path.Contains('%') ? Uri.UnescapeDataString(path) : path;
        try
        {
            var paging = pagingQuery.ResolvePaging();
            var results = _discovery.ListSolutions(decodedPath);
            return Ok(results.ToPagedResult(paging));
        }
        catch (InvalidOperationException ex)
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", ex.Message, "Validation");
        }
    }
}
