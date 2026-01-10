using SilkHat.Api.Services;

namespace SilkHat.Api.Tests.TestHelpers;

public sealed class FakeApiCache : IApiCache
{
    public int InvalidateCount { get; private set; }
    public int GetOrCreateCount { get; private set; }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        GetOrCreateCount++;
        return await factory(cancellationToken);
    }

    public void InvalidateAll()
    {
        InvalidateCount++;
    }
}
