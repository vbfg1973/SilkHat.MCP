using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface ICodeWorkspaceLoader
{
    Task<CodeRepositoryWorkspace> LoadAsync(
        string rootPath,
        IReadOnlyList<SolutionReference> solutions,
        CancellationToken cancellationToken);
}
