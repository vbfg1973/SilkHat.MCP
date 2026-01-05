using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SilkHat.Api.Tests.TestHelpers;

public static class ControllerTestFactory
{
    public static ControllerContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return new ControllerContext { HttpContext = httpContext };
    }
}
