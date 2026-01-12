using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models
{
    public enum CodeFileContentStatus
    {
        Success,
        NotFound,
        InvalidPath
    }

    public sealed record CodeFileContentResult(
        CodeFileContentStatus Status,
        CodeFileContentDto? Content,
        string? Message);
}