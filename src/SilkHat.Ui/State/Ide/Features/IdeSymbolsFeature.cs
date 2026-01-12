using Fluxor;

namespace SilkHat.Ui.State.Ide.Features
{
    public sealed class IdeSymbolsFeature : Feature<IdeSymbolsState>
    {
        public override string GetName()
        {
            return "IdeSymbols";
        }

        protected override IdeSymbolsState GetInitialState()
        {
            return new IdeSymbolsState();
        }
    }
}