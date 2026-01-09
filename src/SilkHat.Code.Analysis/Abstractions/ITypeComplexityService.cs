using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions;

public interface ITypeComplexityService
{
    Task<TypeComplexityResult> GetTypeComplexityAsync(
        CodeSolutionWorkspace solution,
        string docId,
        ComplexityMeasureType measure,
        CancellationToken cancellationToken);
}
