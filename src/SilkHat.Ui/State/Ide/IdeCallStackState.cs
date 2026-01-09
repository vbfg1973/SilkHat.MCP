using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide;

public sealed record IdeCallStackState
{
    public IdeCallStackState()
    {
        Views = new Dictionary<string, IdeCallStackViewState>(StringComparer.OrdinalIgnoreCase);
    }

    public IdeCallStackState(IDictionary<string, IdeCallStackViewState> views)
    {
        Views = new Dictionary<string, IdeCallStackViewState>(views, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IdeCallStackViewState> Views { get; init; }
}

public sealed record IdeCallStackViewState(
    bool IsOpen,
    bool IsLoading,
    bool IsMermaidLoading,
    string? Error,
    string? MermaidError,
    string? DocumentationId,
    string? SymbolKey,
    bool IncludeExternalCalls,
    IReadOnlyList<MethodCallStackNodeModel> Nodes,
    string? MermaidDiagram,
    IdeCallStackShadingMode ShadingMode,
    string? SelectedNodeId)
{
    public static IdeCallStackViewState Empty => new(
        false,
        false,
        false,
        null,
        null,
        null,
        null,
        false,
        Array.Empty<MethodCallStackNodeModel>(),
        null,
        IdeCallStackShadingMode.None,
        null);
}
