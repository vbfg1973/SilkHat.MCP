using SilkHat.Analysis.Services;

namespace SilkHat.Tests.Services
{
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

        [Fact]
        public void GetAll_ReturnsLoadedRepositories()
        {
            var store = new LoadedRepositoryStore();
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            store.SetLoaded(first, "/repo/one");
            store.SetLoaded(second, "/repo/two");

            var loaded = store.GetAll();

            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, repo => repo.ConfigId == first);
            Assert.Contains(loaded, repo => repo.ConfigId == second);
        }
    }
}