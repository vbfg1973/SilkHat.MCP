using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/code/solutions/{solutionId}/namespaces")]
    public sealed class CodeNamespacesController : ApiControllerBase
    {
        private readonly ICodeWorkspaceStore _codeStore;

        public CodeNamespacesController(ICodeWorkspaceStore codeStore)
        {
            _codeStore = codeStore;
        }

        [HttpGet]
        public ActionResult<PagedResult<string>> GetNamespaces(
            Guid id,
            string solutionId,
            [FromQuery] string? prefix,
            [FromQuery] PagingQuery pagingQuery)
        {
            var workspace = _codeStore.Get(id);
            if (workspace is null)
                return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded",
                    "Repository code workspace is not loaded.", "Code");

            var solution = workspace.TryGetSolution(solutionId);
            if (solution is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");

            var paging = pagingQuery.ResolvePaging();
            var namespaces = solution.Namespaces.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(prefix))
                namespaces = namespaces.Where(ns => ns.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            var result = namespaces
                .OrderBy(ns => ns, StringComparer.OrdinalIgnoreCase)
                .ToPagedResult(paging);

            return Ok(result);
        }
    }
}