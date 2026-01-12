using Fluxor;

namespace SilkHat.Ui.State.Ide.Features
{
    public sealed class IdeTreeSettingsFeature : Feature<IdeTreeSettingsState>
    {
        public override string GetName()
        {
            return "IdeTreeSettings";
        }

        protected override IdeTreeSettingsState GetInitialState()
        {
            return IdeTreeSettingsState.Default;
        }
    }
}