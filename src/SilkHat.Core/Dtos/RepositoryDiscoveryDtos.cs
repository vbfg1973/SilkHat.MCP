namespace SilkHat.Core.Dtos;

public sealed record AvailableRepositoryDto(
    string Name,
    string RelativePath,
    string FullPath,
    bool IsGitRepository);

public sealed record RepositoryLoadResultDto(
    Guid RepositoryId,
    string Name,
    bool Loaded,
    string Message);

public sealed record RepositoryGroupLoadResultDto(
    Guid GroupId,
    IReadOnlyList<RepositoryLoadResultDto> Repositories);
