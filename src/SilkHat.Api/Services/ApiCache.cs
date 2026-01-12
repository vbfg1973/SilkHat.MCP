using Microsoft.Extensions.Caching.Hybrid;

namespace SilkHat.Api.Services
{
    public interface IApiCache
    {
        Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken cancellationToken);

        void InvalidateAll();
    }

    public sealed class ApiCache : IApiCache
    {
        private readonly HybridCache _cache;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);
        private long _cacheVersion;

        public ApiCache(HybridCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken cancellationToken)
        {
            var versionedKey = BuildKey(key);
            var options = new HybridCacheEntryOptions
            {
                Expiration = _ttl
            };

            return await _cache.GetOrCreateAsync(
                versionedKey,
                ct => new ValueTask<T>(factory(ct)),
                options,
                cancellationToken: cancellationToken);
        }

        public void InvalidateAll()
        {
            Interlocked.Increment(ref _cacheVersion);
        }

        private string BuildKey(string key)
        {
            var version = Interlocked.Read(ref _cacheVersion);
            return $"v{version}:{key}";
        }
    }
}