using Fluxor;

namespace SilkHat.Ui.Tests.TestHelpers;

public sealed class RecordingDispatcher : IDispatcher
{
    private readonly List<object> _actions = new();

    public IReadOnlyList<object> Actions => _actions;

    public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;

    public void Dispatch(object action)
    {
        _actions.Add(action);
        ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
    }
}
