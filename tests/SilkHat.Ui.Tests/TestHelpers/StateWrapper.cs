using Fluxor;

namespace SilkHat.Ui.Tests.TestHelpers
{
    public sealed class StateWrapper<T> : IState<T>
    {
        public StateWrapper(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public event EventHandler? StateChanged
        {
            add { }
            remove { }
        }
    }
}