using SilkHat.Analysis.Models;

namespace SilkHat.Analysis.Abstractions;

public interface ILoadedRepositoryStore
{
    LoadedRepository? Get(Guid configId);
    IReadOnlyCollection<LoadedRepository> GetAll();
    void SetLoaded(Guid configId, string rootPath);
    bool Unload(Guid configId);
    SemaphoreSlim GetLock(Guid configId);
}
