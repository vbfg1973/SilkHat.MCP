using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/named-types")]
public sealed class CodeNamedTypesController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeNamedTypesController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<NamedTypeDto>> GetNamedTypes(
        Guid id,
        [FromQuery] string? pathPrefix,
        [FromQuery] string? namespacePrefix,
        [FromQuery] string? nameContains,
        [FromQuery] NamedTypeKind? kind,
        [FromQuery] bool? definedOnly)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        IEnumerable<NamedTypeDto> query = workspace.NamedTypes;

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

        return Ok(query.Take(500).ToList());
    }
}
