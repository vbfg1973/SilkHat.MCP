using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Actions;

public sealed record OpenCallStackPopupAction(string SolutionId, string? DocumentationId, string SymbolKey);
public sealed record CloseCallStackPopupAction(string SolutionId);

public sealed record LoadCallStackAction(string SolutionId, Guid ConfigId, string? DocumentationId, string SymbolKey, int? MaxDepth);
public sealed record LoadCallStackSuccessAction(string SolutionId, string? DocumentationId, string SymbolKey, IReadOnlyList<MethodCallStackNodeModel> Nodes);
public sealed record LoadCallStackFailureAction(string SolutionId, string? DocumentationId, string SymbolKey, string Error);

public sealed record LoadCallStackMermaidAction(string SolutionId, Guid ConfigId, string? DocumentationId, string SymbolKey, int? MaxDepth);
public sealed record LoadCallStackMermaidSuccessAction(string SolutionId, string? DocumentationId, string SymbolKey, string Diagram);
public sealed record LoadCallStackMermaidFailureAction(string SolutionId, string? DocumentationId, string SymbolKey, string Error);

public sealed record SaveCallStackDecisionAction(
    string SolutionId,
    Guid ConfigId,
    string InterfaceTypeName,
    string? InterfaceTypeDocumentationId,
    string InterfaceMethodSignature,
    string? InterfaceMethodDocumentationId,
    string ImplementationTypeName,
    string? ImplementationTypeDocumentationId,
    string? ImplementationMethodDocumentationId);

public sealed record SaveCallStackDecisionSuccessAction(
    string SolutionId,
    string InterfaceMethodSignature);

public sealed record SaveCallStackDecisionFailureAction(
    string SolutionId,
    string InterfaceMethodSignature,
    string Error);

public sealed record SelectCallStackNodeAction(string SolutionId, string NodeId);
public sealed record SetCallStackShadingModeAction(string SolutionId, IdeCallStackShadingMode Mode);
