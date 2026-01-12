using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SilkHat.Ui.Components.Ide;
using SilkHat.Ui.Services;

namespace SilkHat.Ui.Tests.Components
{
    public sealed class IdeD3ViewerTests
    {
        [Fact]
        public void RendersWhenLabelProvided()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddScoped<ThemeService>();
            context.Services.AddScoped<VisualizationThemeService>();

            context.RenderComponent<IdeD3Viewer>(parameters =>
                parameters.Add(p => p.Label, "Sample graph"));

            context.JSInterop.VerifyInvoke("silkhatVisualHost.register");
            context.JSInterop.VerifyInvoke("silkhatD3.render");
        }

        [Fact]
        public void ClearsWhenLabelMissing()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddScoped<ThemeService>();
            context.Services.AddScoped<VisualizationThemeService>();

            context.RenderComponent<IdeD3Viewer>(parameters =>
                parameters.Add(p => p.Label, null));

            context.JSInterop.VerifyInvoke("silkhatVisualHost.register");
            context.JSInterop.VerifyInvoke("silkhatD3.clear");
        }
    }
}