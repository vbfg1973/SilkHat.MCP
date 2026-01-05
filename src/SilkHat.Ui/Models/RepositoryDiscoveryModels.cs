namespace SilkHat.Ui.Models;

public sealed record AvailableRepositoryModel(
    string Name,
    string RelativePath,
    string FullPath,
    bool IsGitRepository);

public sealed record RepositoryLoadResultModel(
    Guid RepositoryId,
    string Name,
    bool Loaded,
    string Message);

public sealed record RepositoryGroupLoadResultModel(
    Guid GroupId,
    IReadOnlyList<RepositoryLoadResultModel> Repositories);
