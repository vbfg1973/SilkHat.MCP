using Fluxor;

namespace SilkHat.Ui.Tests.TestHelpers;

public sealed class NoOpActionSubscriber : IActionSubscriber
{
    public void SubscribeToAction<TAction>(object subscriber, Action<TAction> callback)
    {
    }

    public void UnsubscribeFromAllActions(object subscriber)
    {
    }

    public IDisposable? GetActionUnsubscriberAsIDisposable(object subscriber)
    {
        return null;
    }
}
