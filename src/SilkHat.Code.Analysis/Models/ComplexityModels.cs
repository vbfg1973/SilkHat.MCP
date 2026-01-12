using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models
{
    public enum TypeComplexityStatus
    {
        Success = 1,
        NotFound = 2,
        InterfaceNotSupported = 3
    }

    public sealed record TypeComplexityResult(
        TypeComplexityStatus Status,
        ComplexityResultDto? Result);
}