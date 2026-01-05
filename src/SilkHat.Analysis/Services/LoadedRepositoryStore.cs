using System.Collections.Concurrent;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;

namespace SilkHat.Analysis.Services;

public sealed class LoadedRepositoryStore : ILoadedRepositoryStore
{
    private readonly ConcurrentDictionary<Guid, LoadedRepository> _loadedRepositories = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public LoadedRepository? Get(Guid configId)
    {
        return _loadedRepositories.TryGetValue(configId, out var repo) ? repo : null;
    }

    public void SetLoaded(Guid configId, string rootPath)
    {
        _loadedRepositories[configId] = new LoadedRepository(configId, rootPath, DateTimeOffset.UtcNow);
    }

    public bool Unload(Guid configId)
    {
        return _loadedRepositories.TryRemove(configId, out _);
    }

    public SemaphoreSlim GetLock(Guid configId)
    {
        return _locks.GetOrAdd(configId, _ => new SemaphoreSlim(1, 1));
    }
}
