using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Graph;

public sealed class GraphStoreProviderTests
{
    [Fact]
    public void GetOrAdd_ReturnsSameInstance_ForSameSolution()
    {
        IGraphStoreProvider provider = new GraphStoreProvider();

        var first = provider.GetOrAdd("solution-a");
        var second = provider.GetOrAdd("solution-a");

        Assert.Same(first, second);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenMissing()
    {
        IGraphStoreProvider provider = new GraphStoreProvider();

        var found = provider.TryGet("missing", out var store);

        Assert.False(found);
        Assert.Null(store);
    }

    [Fact]
    public void Remove_DropsStore()
    {
        IGraphStoreProvider provider = new GraphStoreProvider();
        provider.GetOrAdd("solution-a");

        provider.Remove("solution-a");

        Assert.False(provider.TryGet("solution-a", out _));
    }
}
