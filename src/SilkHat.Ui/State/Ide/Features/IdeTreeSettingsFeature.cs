using Fluxor;

namespace SilkHat.Ui.State.Ide.Features;

public sealed class IdeTreeSettingsFeature : Feature<IdeTreeSettingsState>
{
    public override string GetName() => "IdeTreeSettings";

    protected override IdeTreeSettingsState GetInitialState() => IdeTreeSettingsState.Default;
}
