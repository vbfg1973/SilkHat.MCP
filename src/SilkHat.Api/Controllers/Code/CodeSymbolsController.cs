using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/symbols")]
public sealed class CodeSymbolsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;
    private readonly IGraphQueryService _graphQueryService;

    public CodeSymbolsController(
        ICodeWorkspaceStore codeStore,
        IGraphQueryService graphQueryService)
    {
        _codeStore = codeStore;
        _graphQueryService = graphQueryService;
    }

    [HttpPost("lookup")]
    public ActionResult<SymbolLookupResultDto> LookupSymbol(Guid id, string solutionId, [FromBody] SymbolLookupRequest request)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var solution = workspace.TryGetSolution(solutionId);
        if (solution is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");
        }

        // Graph-first lookup
        var graphMatch = LookupInGraph(solutionId, request);
        if (graphMatch is not null)
        {
            return Ok(graphMatch);
        }

        NamedTypeDto? namedType = null;

        if (!string.IsNullOrWhiteSpace(request.DocumentationId)
            && solution.NamedTypesByDocId.TryGetValue(request.DocumentationId, out var byDocId))
        {
            namedType = byDocId;
        }
        else if (!string.IsNullOrWhiteSpace(request.DocumentationId))
        {
            var resolved = DocumentationIdUtility.FindTypeByDocumentationId(solution, request.DocumentationId);
            if (resolved is not null)
            {
                namedType = solution.NamedTypes.FirstOrDefault(dto =>
                    string.Equals(dto.FullName, resolved.ToDisplayString(), StringComparison.Ordinal));
            }
        }

        if (namedType is null
            && !string.IsNullOrWhiteSpace(request.SymbolKey)
            && solution.NamedTypesBySymbolKey.TryGetValue(request.SymbolKey, out var bySymbolKey))
        {
            namedType = bySymbolKey;
        }

        if (namedType is null)
        {
            return Ok(new SymbolLookupResultDto(false, request.ExpectedKind, string.Empty, null, null, request.DocumentationId));
        }

        var kind = namedType.Kind.ToString();
        var expectedMatches = ExpectedKindMatches(request.ExpectedKind, namedType.Kind);

        return Ok(new SymbolLookupResultDto(
            expectedMatches,
            kind,
            namedType.Name,
            namedType.Namespace,
            namedType.AssemblyName,
            namedType.DocumentationId));
    }

    private SymbolLookupResultDto? LookupInGraph(string solutionId, SymbolLookupRequest request)
    {
        GraphNodeDto? node = null;
        if (!string.IsNullOrWhiteSpace(request.DocumentationId)
            && _graphQueryService.TryGetNodeByKey(solutionId, request.DocumentationId, out var byDoc))
        {
            node = byDoc;
        }
        else if (!string.IsNullOrWhiteSpace(request.SymbolKey)
            && _graphQueryService.TryGetNodeByKey(solutionId, request.SymbolKey, out var bySymbol))
        {
            node = bySymbol;
        }

        if (node is null || node.Kind != GraphNodeKind.NamedType)
        {
            return null;
        }

        var attributes = node.Attributes ?? new Dictionary<string, string>();
        var kind = attributes.TryGetValue("RealType", out var realType)
            ? realType
            : "NamedType";
        var expectedMatches = ExpectedKindMatches(request.ExpectedKind, kind);

        var name = node.Label ?? attributes.GetValueOrDefault("Name") ?? node.Key;
        var ns = attributes.GetValueOrDefault("Namespace");
        var assembly = attributes.GetValueOrDefault("AssemblyName");
        var docId = attributes.GetValueOrDefault("DocumentationId");

        return new SymbolLookupResultDto(
            expectedMatches,
            kind,
            name,
            ns,
            assembly,
            docId);
    }

    private static bool ExpectedKindMatches(string expectedKind, NamedTypeKind actualKind)
    {
        if (string.IsNullOrWhiteSpace(expectedKind))
        {
            return true;
        }

        if (string.Equals(expectedKind, "NamedType", StringComparison.OrdinalIgnoreCase)
            || string.Equals(expectedKind, "NamedTypeSymbol", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(expectedKind, actualKind.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ExpectedKindMatches(string expectedKind, string actualKind)
    {
        if (string.IsNullOrWhiteSpace(expectedKind))
        {
            return true;
        }

        if (string.Equals(expectedKind, "NamedType", StringComparison.OrdinalIgnoreCase)
            || string.Equals(expectedKind, "NamedTypeSymbol", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(expectedKind, actualKind, StringComparison.OrdinalIgnoreCase);
    }
}
