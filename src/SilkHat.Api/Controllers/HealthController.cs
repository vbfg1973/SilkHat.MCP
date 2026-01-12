using Microsoft.AspNetCore.Mvc;

namespace SilkHat.Api.Controllers
{
    [Route("api/health")]
    public sealed class HealthController : ApiControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("OK");
        }
    }
}