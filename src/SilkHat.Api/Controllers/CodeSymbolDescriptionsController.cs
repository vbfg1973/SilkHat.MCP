using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/symbols")]
public sealed class CodeSymbolDescriptionsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _store;
    private readonly ISymbolDescriptionService _symbols;

    public CodeSymbolDescriptionsController(ICodeWorkspaceStore store, ISymbolDescriptionService symbols)
    {
        _store = store;
        _symbols = symbols;
    }

    [HttpGet("types/{docId}")]
    public async Task<ActionResult<NamedTypeDescriptionDto>> GetNamedType(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribeNamedTypeAsync(solution, workspace, documentationId, token));
    }

    [HttpGet("methods/{docId}")]
    public async Task<ActionResult<MethodDescriptionDto>> GetMethod(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribeMethodAsync(solution, workspace, documentationId, token));
    }

    [HttpGet("properties/{docId}")]
    public async Task<ActionResult<PropertyDescriptionDto>> GetProperty(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribePropertyAsync(solution, workspace, documentationId, token));
    }

    [HttpGet("fields/{docId}")]
    public async Task<ActionResult<FieldDescriptionDto>> GetField(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribeFieldAsync(solution, workspace, documentationId, token));
    }

    [HttpGet("events/{docId}")]
    public async Task<ActionResult<EventDescriptionDto>> GetEvent(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribeEventAsync(solution, workspace, documentationId, token));
    }

    [HttpGet("namespaces/{docId}")]
    public async Task<ActionResult<NamespaceDescriptionDto>> GetNamespace(
        Guid id,
        string solutionId,
        string docId,
        CancellationToken cancellationToken)
    {
        return await Handle(docId, id, solutionId, cancellationToken,
            (solution, workspace, documentationId, token) =>
                _symbols.DescribeNamespaceAsync(solution, workspace, documentationId, token));
    }

    private async Task<ActionResult<T>> Handle<T>(
        string docId,
        Guid id,
        string solutionId,
        CancellationToken cancellationToken,
        Func<CodeSolutionWorkspace, CodeRepositoryWorkspace, string, CancellationToken,
            Task<SymbolDescriptionResult<T>>> handler)
    {
        if (string.IsNullOrWhiteSpace(docId))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "DocumentationId is required.", "Validation");
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

        var result = await handler(solution, workspace, docId, cancellationToken);
        return result.Status switch
        {
            SymbolDescriptionStatus.Success when result.Description is not null => Ok(result.Description),
            SymbolDescriptionStatus.Unsupported => ProblemWithCategory(StatusCodes.Status400BadRequest, "Unsupported", result.Error ?? "DocumentationId is not supported.", "Code"),
            _ => ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", result.Error ?? "Symbol not found.", "Code")
        };
    }
}
