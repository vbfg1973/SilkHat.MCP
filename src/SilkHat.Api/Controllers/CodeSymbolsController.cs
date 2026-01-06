using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
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

        if (!solution.NamedTypesBySymbolKey.TryGetValue(request.SymbolKey, out var namedType))
        {
            return Ok(new SymbolLookupResultDto(false, request.ExpectedKind, string.Empty, null, null));
        }

        var kind = namedType.Kind.ToString();
        var expectedMatches = ExpectedKindMatches(request.ExpectedKind, namedType.Kind);

        return Ok(new SymbolLookupResultDto(expectedMatches, kind, namedType.Name, namedType.Namespace, namedType.AssemblyName));
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
