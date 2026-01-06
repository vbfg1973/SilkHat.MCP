namespace SilkHat.Ui.Models;

public enum CodeTreeEntryType
{
    Project = 0,
    Directory = 1,
    File = 2
}

public sealed record CodeTreeEntryModel(
    string RepositoryPath,
    string DisplayPath,
    string Name,
    CodeTreeEntryType Type,
    string ProjectKey,
    string ProjectName);
