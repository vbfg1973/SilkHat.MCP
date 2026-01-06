namespace SilkHat.Code.Core.Dtos;

public enum CodeTreeEntryType
{
    Project = 0,
    Directory = 1,
    File = 2
}

public sealed record CodeTreeEntryDto(
    string RepositoryPath,
    string DisplayPath,
    string Name,
    CodeTreeEntryType Type,
    string ProjectKey,
    string ProjectName);
