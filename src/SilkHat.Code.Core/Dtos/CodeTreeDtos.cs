namespace SilkHat.Code.Core.Dtos
{
    public enum CodeTreeEntryType
    {
        Project = 0,
        Directory = 1,
        File = 2,
        Type = 3,
        Member = 4
    }

    public enum CodeTreeAnnotationKind
    {
        CognitiveComplexity = 0,
        CyclomaticComplexity = 1,
        IndentationComplexity = 2,
        FileAuthorCount = 3,
        FileChangeCount = 4
    }

    public enum CodeTreeFilterOperator
    {
        LessThanOrEqual = 0,
        GreaterThan = 1
    }

    public sealed record CodeTreeEntryDto(
        string RepositoryPath,
        string DisplayPath,
        string Name,
        CodeTreeEntryType Type,
        string ProjectKey,
        string ProjectName,
        string? DocumentationId,
        string? SymbolKind,
        string? RealType,
        CodeLocationDto? Location,
        CodeTreeAnnotationKind? AnnotationKind,
        int? AnnotationValue);
}