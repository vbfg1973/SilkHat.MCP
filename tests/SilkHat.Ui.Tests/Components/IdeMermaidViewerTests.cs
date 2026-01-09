using Bunit;
using Microsoft.JSInterop;
using SilkHat.Ui.Components.Ide;

namespace SilkHat.Ui.Tests.Components;

public sealed class IdeMermaidViewerTests
{
    [Fact]
    public void RendersDiagramWhenProvided()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        context.RenderComponent<IdeMermaidViewer>(parameters =>
            parameters.Add(p => p.Diagram, "graph TD; A-->B"));

        context.JSInterop.VerifyInvoke("silkhatMermaid.render");
    }

    [Fact]
    public void ClearsDiagramWhenMissing()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        context.RenderComponent<IdeMermaidViewer>(parameters =>
            parameters.Add(p => p.Diagram, null));

        context.JSInterop.VerifyInvoke("silkhatMermaid.clear");
    }
}
