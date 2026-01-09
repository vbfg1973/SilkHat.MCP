using Fluxor;

namespace SilkHat.Ui.State.Ide.Features;

public sealed class IdeDecisionsFeature : Feature<IdeDecisionsState>
{
    public override string GetName() => "IdeDecisions";

    protected override IdeDecisionsState GetInitialState()
    {
        return new IdeDecisionsState();
    }
}
