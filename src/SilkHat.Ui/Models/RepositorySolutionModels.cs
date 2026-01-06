namespace SilkHat.Ui.Models;

public sealed record RepositorySolutionModel(
    string RelativePath,
    bool IsEnabled,
    string SolutionId);

public sealed record AvailableRepositorySolutionModel(
    string RelativePath,
    string SolutionId);
