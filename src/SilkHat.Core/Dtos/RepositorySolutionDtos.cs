namespace SilkHat.Core.Dtos;

public sealed record RepositorySolutionDto(
    string RelativePath,
    bool IsEnabled);

public sealed record AvailableRepositorySolutionDto(
    string RelativePath);
