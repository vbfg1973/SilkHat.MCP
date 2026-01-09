using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IMethodComplexityService
{
    Task<ComplexityResultDto?> GetMethodComplexityAsync(
        CodeSolutionWorkspace solution,
        string docId,
        ComplexityMeasureType measure,
        CancellationToken cancellationToken);
}
