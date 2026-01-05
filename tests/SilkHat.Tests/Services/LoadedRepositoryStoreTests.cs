using SilkHat.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class LoadedRepositoryStoreTests
{
    [Fact]
    public void SetLoaded_ThenGet_ReturnsRepository()
    {
        var store = new LoadedRepositoryStore();
        var id = Guid.NewGuid();

        store.SetLoaded(id, "/repo");

        var loaded = store.Get(id);
        Assert.NotNull(loaded);
        Assert.Equal(id, loaded!.ConfigId);
    }

    [Fact]
    public void Unload_RemovesRepository()
    {
        var store = new LoadedRepositoryStore();
        var id = Guid.NewGuid();

        store.SetLoaded(id, "/repo");

        var removed = store.Unload(id);

        Assert.True(removed);
        Assert.Null(store.Get(id));
    }

    [Fact]
    public void GetLock_ReturnsSameSemaphorePerConfig()
    {
        var store = new LoadedRepositoryStore();
        var id = Guid.NewGuid();

        var first = store.GetLock(id);
        var second = store.GetLock(id);

        Assert.Same(first, second);
    }
}
