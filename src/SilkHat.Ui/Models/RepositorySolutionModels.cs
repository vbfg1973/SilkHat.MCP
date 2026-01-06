namespace SilkHat.Ui.Models;

public sealed record RepositorySolutionModel(
    string RelativePath,
    bool IsEnabled);

public sealed record AvailableRepositorySolutionModel(
    string RelativePath);
