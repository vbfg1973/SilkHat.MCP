using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions;

public interface ICodeTreeService
{
    IReadOnlyList<CodeTreeEntryDto> GetTree(CodeSolutionWorkspace solution, string? parentId);
}
