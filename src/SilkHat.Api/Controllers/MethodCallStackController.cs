using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/methods/call-stack")]
public sealed class MethodCallStackController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;
    private readonly IMethodCallStackService _callStackService;
    private readonly IMethodCallStackMermaidService _mermaidService;

    public MethodCallStackController(
        ICodeWorkspaceStore store,
        IMethodCallStackService callStackService,
        IMethodCallStackMermaidService mermaidService)
    {
        _store = store;
        _callStackService = callStackService;
        _mermaidService = mermaidService;
    }

    [HttpPost]
    public async Task<ActionResult<MethodCallStackResponseDto>> GetCallStack(
        Guid id,
        string solutionId,
        [FromBody] MethodCallStackRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();
            var message = errors.Count > 0 ? string.Join(" ", errors) : "Validation failed.";
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", message, "Validation");
        }

        var workspace = _store.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var solution = workspace.TryGetSolution(solutionId);
        if (solution is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");
        }

        var result = await _callStackService.BuildCallStackAsync(
            workspace,
            solution,
            id,
            request.DocumentationId,
            request.SymbolKey,
            request.MaxDepth,
            request.IncludeExternalCalls,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Call Stack Error", result.Error, "Code");
        }

        var nodes = result.Nodes.Select(MapNode).ToList();
        return Ok(new MethodCallStackResponseDto(nodes));
    }

    [HttpPost("mermaid")]
    public async Task<ActionResult<MethodCallStackMermaidDto>> GetCallStackMermaid(
        Guid id,
        string solutionId,
        [FromBody] MethodCallStackRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();
            var message = errors.Count > 0 ? string.Join(" ", errors) : "Validation failed.";
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", message, "Validation");
        }

        var workspace = _store.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var solution = workspace.TryGetSolution(solutionId);
        if (solution is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");
        }

        var result = await _callStackService.BuildCallStackAsync(
            workspace,
            solution,
            id,
            request.DocumentationId,
            request.SymbolKey,
            request.MaxDepth,
            request.IncludeExternalCalls,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Call Stack Error", result.Error, "Code");
        }

        var diagram = _mermaidService.BuildDiagram(result.Nodes);
        return Ok(new MethodCallStackMermaidDto(diagram));
    }

    private static MethodCallStackNodeDto MapNode(MethodCallStackNode node)
    {
        var callSite = new CodeLocationDto(
            node.CallSite.Path,
            new CodeTextSpanDto(node.CallSite.SpanStart, node.CallSite.SpanLength),
            new CodeLineSpanDto(
                node.CallSite.StartLine,
                node.CallSite.StartColumn,
                node.CallSite.EndLine,
                node.CallSite.EndColumn));

        var decision = node.Decision is null
            ? null
            : new DecisionInfoDto(node.Decision.Id, node.Decision.Type);

        return new MethodCallStackNodeDto(
            node.NodeId,
            node.Depth,
            node.Index,
            node.CallerNamespace,
            node.CallerTypeName,
            node.CallerMethodName,
            node.CallerParameterTypes,
            node.CallerFullyQualifiedMethodName,
            node.Namespace,
            node.TypeName,
            node.MethodName,
            node.ParameterTypes,
            node.FullyQualifiedMethodName,
            node.ParentNodeId,
            callSite,
            decision,
            node.IsInterfaceTarget,
            node.InterfaceTypeName,
            node.InterfaceTypeDocumentationId,
            node.InterfaceMethodDocumentationId,
            node.ResolvedTypeName,
            node.ResolvedTypeDocumentationId,
            node.ResolvedMethodDocumentationId,
            node.DecisionRequired,
            node.CandidateTypeNames,
            node.CandidateMethodDocumentationIds);
    }

}
