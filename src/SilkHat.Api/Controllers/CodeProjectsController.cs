using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/projects")]
public sealed class CodeProjectsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeProjectsController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet]
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

    [HttpGet("{projectKey}")]
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

    [HttpGet("{projectKey}/references")]
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

    [HttpGet("{projectKey}/referenced-by")]
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
}
