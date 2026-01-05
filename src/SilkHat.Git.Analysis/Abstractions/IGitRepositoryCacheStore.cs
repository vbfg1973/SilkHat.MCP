using SilkHat.Git.Analysis.Models;

namespace SilkHat.Git.Analysis.Abstractions;

public interface IGitRepositoryCacheStore
{
    GitRepositoryCache GetOrCreate(Guid configId);
    bool Remove(Guid configId);
}
