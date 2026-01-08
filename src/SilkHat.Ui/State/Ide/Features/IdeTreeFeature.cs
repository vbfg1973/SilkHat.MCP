using Fluxor;

namespace SilkHat.Ui.State.Ide.Features;

public sealed class IdeTreeFeature : Feature<IdeTreeState>
{
    public override string GetName() => "IdeTree";

    protected override IdeTreeState GetInitialState()
    {
        return new IdeTreeState();
    }
}
