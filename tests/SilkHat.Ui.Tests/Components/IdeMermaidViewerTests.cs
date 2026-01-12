using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SilkHat.Ui.Components.Ide;
using SilkHat.Ui.Services;

namespace SilkHat.Ui.Tests.Components
{
    public sealed class IdeMermaidViewerTests
    {
        [Fact]
        public void RendersDiagramWhenProvided()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddScoped<ThemeService>();
            context.Services.AddScoped<VisualizationThemeService>();

            context.RenderComponent<IdeMermaidViewer>(parameters =>
                parameters.Add(p => p.Diagram, "graph TD; A-->B"));

            context.JSInterop.VerifyInvoke("silkhatVisualHost.register");
            context.JSInterop.VerifyInvoke("silkhatMermaid.render");
            var invocation = context.JSInterop.Invocations.First(item => item.Identifier == "silkhatMermaid.render");
            Assert.True(invocation.Arguments.Count >= 3);
            Assert.NotNull(invocation.Arguments[2]);
        }

        [Fact]
        public void ClearsDiagramWhenMissing()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddScoped<ThemeService>();
            context.Services.AddScoped<VisualizationThemeService>();

            context.RenderComponent<IdeMermaidViewer>(parameters =>
                parameters.Add(p => p.Diagram, null));

            context.JSInterop.VerifyInvoke("silkhatVisualHost.register");
            context.JSInterop.VerifyInvoke("silkhatMermaid.clear");
        }

        [Fact]
        public async Task RendersAgainWhenRequested()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddScoped<ThemeService>();
            context.Services.AddScoped<VisualizationThemeService>();

            var component = context.RenderComponent<IdeMermaidViewer>(parameters =>
                parameters.Add(p => p.Diagram, "graph TD; A-->B"));

            var initialCount = context.JSInterop.Invocations.Count(item => item.Identifier == "silkhatMermaid.render");
            await component.Instance.NotifyRenderRequested();
            var finalCount = context.JSInterop.Invocations.Count(item => item.Identifier == "silkhatMermaid.render");

            Assert.True(finalCount > initialCount);
        }
    }
}