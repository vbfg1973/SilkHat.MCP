using Fluxor;

namespace SilkHat.Ui.State.Ide.Features
{
    public sealed class IdeCallStackFeature : Feature<IdeCallStackState>
    {
        public override string GetName()
        {
            return "IdeCallStack";
        }

        protected override IdeCallStackState GetInitialState()
        {
            return new IdeCallStackState();
        }
    }
}