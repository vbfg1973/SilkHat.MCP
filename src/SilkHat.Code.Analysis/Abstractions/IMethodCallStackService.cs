using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IMethodCallStackService
{
    Task<MethodCallStackResult> BuildCallStackAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        string methodSymbolKey,
        int? maxDepth,
        CancellationToken cancellationToken);
}
