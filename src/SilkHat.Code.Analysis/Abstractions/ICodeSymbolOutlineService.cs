using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface ICodeSymbolOutlineService
{
    Task<CodeFileSymbolsResult> GetFileSymbolsAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string repositoryPath,
        CancellationToken cancellationToken);
}
