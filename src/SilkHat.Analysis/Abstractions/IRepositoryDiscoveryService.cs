using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Abstractions;

public interface IRepositoryDiscoveryService
{
    bool IsConfigured { get; }
    string? RepositoryRoot { get; }
    IReadOnlyList<AvailableRepositoryDto> ListAvailableRepositories();
    bool TryValidateRepositoryPath(string rootPath, out string? error);
}
