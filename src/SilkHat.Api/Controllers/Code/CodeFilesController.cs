using Microsoft.AspNetCore.Mvc;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Controllers
{
    [Route("api/repositories/{id:guid}/code/solutions/{solutionId}/files")]
    public sealed class CodeFilesController : ApiControllerBase
    {
        private readonly ICodeFileService _fileService;
        private readonly ICodeWorkspaceStore _store;

        public CodeFilesController(ICodeWorkspaceStore store, ICodeFileService fileService)
        {
            _store = store;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<ActionResult<CodeFileContentDto>> GetFile(
            Guid id,
            string solutionId,
            [FromQuery] CodeFileQuery query,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .ToList();
                var message = errors.Count > 0 ? string.Join(" ", errors) : "Validation failed.";
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", message, "Validation");
            }

            var workspace = _store.Get(id);
            if (workspace is null)
                return ProblemWithCategory(StatusCodes.Status409Conflict, "Repository Not Loaded",
                    "Repository is not loaded.", "Code");

            var solution = workspace.TryGetSolution(solutionId);
            if (solution is null)
                return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Solution not found.", "Code");

            var result = await _fileService.GetFileAsync(workspace, solution, query.Path!, cancellationToken);
            return result.Status switch
            {
                CodeFileContentStatus.Success => Ok(result.Content),
                CodeFileContentStatus.NotFound => ProblemWithCategory(
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    result.Message ?? "File not found.",
                    "Code"),
                CodeFileContentStatus.InvalidPath => ProblemWithCategory(
                    StatusCodes.Status400BadRequest,
                    "Validation Failed",
                    result.Message ?? "Invalid file path.",
                    "Validation"),
                _ => ProblemWithCategory(
                    StatusCodes.Status500InternalServerError,
                    "Internal Error",
                    "Unable to read file.",
                    "Code")
            };
        }
    }
}