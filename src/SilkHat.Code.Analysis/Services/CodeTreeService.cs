using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeTreeService : ICodeTreeService
{
    public IReadOnlyList<CodeTreeEntryDto> GetTree(CodeSolutionWorkspace solution, string? parentId)
    {
        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        var key = string.IsNullOrWhiteSpace(parentId) ? string.Empty : parentId.Trim().TrimEnd('/');
        if (solution.TreeChildrenByParent.TryGetValue(key, out var children))
        {
            return children;
        }

        return Array.Empty<CodeTreeEntryDto>();
    }
}
