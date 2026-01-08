using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/named-types")]
public sealed class CodeNamedTypesController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeNamedTypesController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet]
    public ActionResult<PagedResult<NamedTypeDto>> GetNamedTypes(
        Guid id,
        string solutionId,
        [FromQuery] string? pathPrefix,
        [FromQuery] string? namespacePrefix,
        [FromQuery] string? nameContains,
        [FromQuery] NamedTypeKind? kind,
        [FromQuery] bool? definedOnly,
        [FromQuery] PagingQuery pagingQuery)
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

        var paging = pagingQuery.ResolvePaging();
        IEnumerable<NamedTypeDto> query = solution.NamedTypes;

        if (!string.IsNullOrWhiteSpace(pathPrefix))
        {
            query = query.Where(type => type.FilePath?.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(namespacePrefix))
        {
            query = query.Where(type => type.Namespace.StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            query = query.Where(type => type.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));
        }

        if (kind.HasValue)
        {
            query = query.Where(type => type.Kind == kind.Value);
        }

        if (definedOnly == true)
        {
            query = query.Where(type => !type.IsExternal);
        }

        var result = query
            .OrderBy(type => type.Namespace, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ToPagedResult(paging);

        return Ok(result);
    }
}
