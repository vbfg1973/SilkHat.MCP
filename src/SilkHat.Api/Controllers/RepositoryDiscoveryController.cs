using Microsoft.AspNetCore.Mvc;
using SilkHat.Analysis.Abstractions;
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
    public ActionResult<IReadOnlyList<AvailableRepositoryDto>> GetAvailable()
    {
        try
        {
            return Ok(_discovery.ListAvailableRepositories());
        }
        catch (InvalidOperationException ex)
        {
            return ProblemWithCategory(StatusCodes.Status500InternalServerError, "Configuration Error", ex.Message, "Configuration");
        }
    }
}
