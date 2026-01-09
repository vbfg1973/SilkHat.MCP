using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Actions;

public sealed record OpenSymbolPopupAction(string SolutionId);

public sealed record CloseSymbolPopupAction(string SolutionId);

public sealed record LoadFileSymbolsAction(string SolutionId, Guid ConfigId, string RepositoryPath);

public sealed record LoadFileSymbolsSuccessAction(
    string SolutionId,
    string RepositoryPath,
    IReadOnlyList<CodeSymbolOutlineNodeModel> Nodes);

public sealed record LoadFileSymbolsFailureAction(string SolutionId, string RepositoryPath, string Error);

public sealed record SelectSymbolNodeAction(string SolutionId, string? DocumentationId, string SymbolKey);
