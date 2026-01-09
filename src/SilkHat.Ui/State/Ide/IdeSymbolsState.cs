using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide;

public sealed record IdeSymbolsState
{
    public IdeSymbolsState()
    {
        Views = new Dictionary<string, IdeSymbolsViewState>(StringComparer.OrdinalIgnoreCase);
    }

    public IdeSymbolsState(IDictionary<string, IdeSymbolsViewState> views)
    {
        Views = new Dictionary<string, IdeSymbolsViewState>(views, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IdeSymbolsViewState> Views { get; init; }
}

public sealed record IdeSymbolsViewState(
    bool IsOpen,
    bool IsLoading,
    string? Error,
    string? RepositoryPath,
    IReadOnlyList<CodeSymbolOutlineNodeModel> Nodes,
    string? SelectedDocumentationId,
    string? SelectedSymbolKey)
{
    public static IdeSymbolsViewState Empty => new(
        false,
        false,
        null,
        null,
        Array.Empty<CodeSymbolOutlineNodeModel>(),
        null,
        null);
}
