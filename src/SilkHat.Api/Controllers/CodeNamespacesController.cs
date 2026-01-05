using Microsoft.AspNetCore.Mvc;
using SilkHat.Code.Analysis.Abstractions;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/namespaces")]
public sealed class CodeNamespacesController : ApiControllerBase
{
    private readonly ICodeWorkspaceStore _codeStore;

    public CodeNamespacesController(ICodeWorkspaceStore codeStore)
    {
        _codeStore = codeStore;
    }

    [HttpGet]
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
}
