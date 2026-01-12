using Fluxor;

namespace SilkHat.Ui.State.Ide.Features
{
    public sealed class IdeTabsFeature : Feature<IdeTabsState>
    {
        public override string GetName()
        {
            return "IdeTabs";
        }

        protected override IdeTabsState GetInitialState()
        {
            return new IdeTabsState();
        }
    }
}