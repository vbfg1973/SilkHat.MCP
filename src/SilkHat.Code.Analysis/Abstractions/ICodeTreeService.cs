using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface ICodeTreeService
    {
        Task<IReadOnlyList<CodeTreeEntryDto>> GetTreeAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            string? parentId,
            CancellationToken cancellationToken);
    }
}