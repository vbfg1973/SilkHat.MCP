using Fluxor;

namespace SilkHat.Ui.State.Ide.Features
{
    public sealed class IdeSolutionsFeature : Feature<IdeSolutionsState>
    {
        public override string GetName()
        {
            return "IdeSolutions";
        }

        protected override IdeSolutionsState GetInitialState()
        {
            return new IdeSolutionsState();
        }
    }
}