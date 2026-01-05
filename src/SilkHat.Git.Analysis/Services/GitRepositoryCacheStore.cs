using System.Collections.Concurrent;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;

namespace SilkHat.Git.Analysis.Services;

public sealed class GitRepositoryCacheStore : IGitRepositoryCacheStore
{
    private readonly ConcurrentDictionary<Guid, GitRepositoryCache> _cache = new();

    public GitRepositoryCache GetOrCreate(Guid configId)
    {
        return _cache.GetOrAdd(configId, _ => new GitRepositoryCache());
    }

    public bool Remove(Guid configId)
    {
        return _cache.TryRemove(configId, out _);
    }
}
