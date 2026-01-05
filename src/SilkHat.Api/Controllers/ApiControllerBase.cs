using Microsoft.AspNetCore.Mvc;

namespace SilkHat.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ProblemWithCategory(int statusCode, string title, string detail, string category)
    {
        var correlationId = HttpContext.Items.TryGetValue("CorrelationId", out var value)
            ? value?.ToString()
            : null;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["category"] = category;

        return StatusCode(statusCode, problem);
    }
}
