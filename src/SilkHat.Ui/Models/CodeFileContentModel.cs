namespace SilkHat.Ui.Models;

public sealed record CodeFileContentModel(
    string RepositoryPath,
    string DisplayPath,
    string Content);
