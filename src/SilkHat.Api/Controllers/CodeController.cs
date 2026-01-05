using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code")]
public sealed class CodeController : ApiControllerBase
{
    private readonly CodeWorkspaceStore _codeStore;

    public CodeController(CodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet("projects")]
    public ActionResult<IReadOnlyList<CodeProjectDto>> GetProjects(Guid id, [FromQuery] string? name)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var projects = workspace.Projects.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            projects = projects.Where(project => project.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        var result = projects
            .Select(project => new CodeProjectDto(project.ProjectKey, project.Name, project.Language, project.AssemblyName))
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(result);
    }

    [HttpGet("projects/{projectKey}")]
    public ActionResult<CodeProjectDto> GetProject(Guid id, string projectKey)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        if (!workspace.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        return Ok(new CodeProjectDto(project.ProjectKey, project.Name, project.Language, project.AssemblyName));
    }

    [HttpGet("projects/{projectKey}/references")]
    public ActionResult<IReadOnlyList<CodeProjectReferenceDto>> GetProjectReferences(Guid id, string projectKey)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        if (!workspace.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        return Ok(project.References);
    }

    [HttpGet("projects/{projectKey}/referenced-by")]
    public ActionResult<IReadOnlyList<CodeProjectReferenceDto>> GetProjectReferencedBy(Guid id, string projectKey)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        if (!workspace.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        return Ok(project.ReferencedBy);
    }

    [HttpGet("namespaces")]
    public ActionResult<IReadOnlyList<string>> GetNamespaces(Guid id, [FromQuery] string? prefix)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        var namespaces = workspace.Namespaces.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            namespaces = namespaces.Where(ns => ns.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(namespaces.OrderBy(ns => ns, StringComparer.OrdinalIgnoreCase).ToList());
    }

    [HttpGet("named-types")]
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

    [HttpPost("symbols/lookup")]
    public ActionResult<SymbolLookupResultDto> LookupSymbol(Guid id, [FromBody] SymbolLookupRequest request)
    {
        var workspace = _codeStore.Get(id);
        if (workspace is null)
        {
            return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded", "Repository code workspace is not loaded.", "Code");
        }

        if (!workspace.NamedTypesBySymbolKey.TryGetValue(request.SymbolKey, out var namedType))
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
