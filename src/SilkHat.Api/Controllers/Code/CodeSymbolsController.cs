using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/symbols")]
public sealed class CodeSymbolsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeSymbolsController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
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
}
