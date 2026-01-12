namespace SilkHat.Code.Core.Dtos
{
    public sealed record CodeTextSpanDto(int Start, int Length);

    public sealed record CodeLineSpanDto(
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn);

    public sealed record CodeLocationDto(
        string Path,
        CodeTextSpanDto Span,
        CodeLineSpanDto LineSpan);
}