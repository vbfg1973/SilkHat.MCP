using Fluxor;

namespace SilkHat.Ui.State.Ide.Features;

public sealed class IdeLayoutFeature : Feature<IdeLayoutState>
{
    public override string GetName() => "IdeLayout";

    protected override IdeLayoutState GetInitialState()
    {
        return new IdeLayoutState();
    }
}
