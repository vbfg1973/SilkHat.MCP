namespace SilkHat.Code.Core.Dtos;

public sealed record CodeFileContentDto(
    string RepositoryPath,
    string DisplayPath,
    string Content);
