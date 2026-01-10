using System.Collections.Concurrent;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Services;

public interface ICodeTreeMetricsCacheStore
{
    bool TryGet(
        Guid configId,
        string solutionId,
        CodeTreeAnnotationKind kind,
        out CodeTreeMetricValues? metrics);

    void Set(
        Guid configId,
        string solutionId,
        CodeTreeAnnotationKind kind,
        CodeTreeMetricValues metrics);

    void Clear(Guid configId);
}

public sealed class CodeTreeMetricsCacheStore : ICodeTreeMetricsCacheStore
{
    private readonly ConcurrentDictionary<CacheKey, CodeTreeMetricValues> _cache = new();

    public bool TryGet(
        Guid configId,
        string solutionId,
        CodeTreeAnnotationKind kind,
        out CodeTreeMetricValues? metrics)
    {
        if (_cache.TryGetValue(new CacheKey(configId, solutionId, kind), out var stored))
        {
            metrics = stored;
            return true;
        }

        metrics = null;
        return false;
    }

    public void Set(
        Guid configId,
        string solutionId,
        CodeTreeAnnotationKind kind,
        CodeTreeMetricValues metrics)
    {
        _cache[new CacheKey(configId, solutionId, kind)] = metrics;
    }

    public void Clear(Guid configId)
    {
        foreach (var key in _cache.Keys)
        {
            if (key.ConfigId == configId)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private readonly record struct CacheKey(Guid ConfigId, string SolutionId, CodeTreeAnnotationKind Kind);
}
