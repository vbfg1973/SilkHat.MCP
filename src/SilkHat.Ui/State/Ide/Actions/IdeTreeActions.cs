using MudBlazor;
using SilkHat.Ui.Models;

namespace SilkHat.Ui.State.Ide.Actions
{
    public sealed record LoadTreeRootAction(string SolutionId, Guid ConfigId);

    public sealed record LoadTreeRootSuccessAction(
        string SolutionId,
        IReadOnlyList<TreeItemData<CodeTreeEntryModel>> Items);

    public sealed record LoadTreeRootFailureAction(string SolutionId, string Error);

    public sealed record LoadTreeChildrenAction(
        string SolutionId,
        Guid ConfigId,
        string ParentId,
        TaskCompletionSource<IReadOnlyCollection<TreeItemData<CodeTreeEntryModel>>> Completion);

    public sealed record LoadTreeChildrenSuccessAction(
        string SolutionId,
        string ParentId,
        IReadOnlyList<TreeItemData<CodeTreeEntryModel>> Items);

    public sealed record LoadTreeChildrenFailureAction(string SolutionId, string ParentId, string Error);
}