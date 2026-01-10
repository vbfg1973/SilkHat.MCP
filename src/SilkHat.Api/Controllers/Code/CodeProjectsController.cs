using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/projects")]
public sealed class CodeProjectsController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeProjectsController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet]
    public ActionResult<PagedResult<CodeProjectDto>> GetProjects(
        Guid id,
        string solutionId,
        [FromQuery] string? name,
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
        var projects = solution.Projects.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            projects = projects.Where(project => project.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        var result = projects
            .Select(project => new CodeProjectDto(project.ProjectKey, project.Name, project.Language, project.AssemblyName))
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToPagedResult(paging);

        return Ok(result);
    }

    [HttpGet("{projectKey}")]
    public ActionResult<CodeProjectDto> GetProject(Guid id, string solutionId, string projectKey)
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

        if (!solution.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        return Ok(new CodeProjectDto(project.ProjectKey, project.Name, project.Language, project.AssemblyName));
    }

    [HttpGet("{projectKey}/references")]
    public ActionResult<PagedResult<CodeProjectReferenceDto>> GetProjectReferences(
        Guid id,
        string solutionId,
        string projectKey,
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

        if (!solution.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        var paging = pagingQuery.ResolvePaging();
        return Ok(project.References.ToPagedResult(paging));
    }

    [HttpGet("{projectKey}/referenced-by")]
    public ActionResult<PagedResult<CodeProjectReferenceDto>> GetProjectReferencedBy(
        Guid id,
        string solutionId,
        string projectKey,
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

        if (!solution.Projects.TryGetValue(projectKey, out var project))
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Project not found.", "Code");
        }

        var paging = pagingQuery.ResolvePaging();
        return Ok(project.ReferencedBy.ToPagedResult(paging));
    }
}
