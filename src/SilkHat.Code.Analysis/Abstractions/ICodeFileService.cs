using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface ICodeFileService
{
    Task<CodeFileContentResult> GetFileAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string displayPath,
        CancellationToken cancellationToken);
}
