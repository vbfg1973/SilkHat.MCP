using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Controllers;
using SilkHat.Api.Tests.TestHelpers;

namespace SilkHat.Api.Tests.Controllers;

public sealed class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOk()
    {
        var controller = new HealthController
        {
            ControllerContext = ControllerTestFactory.CreateContext()
        };

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("OK", ok.Value);
    }
}
