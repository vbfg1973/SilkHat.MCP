using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using SilkHat.Ui.Components.Ide;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Models;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.Pages
{
    public sealed class IdeTests
    {
        [Fact]
        public void Ide_RendersTreeEntries_AfterRepositorySelection()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddMudServices();
            context.Services.AddLogging();
            var solutionId = "solution-1";
            var solutionsState = new IdeSolutionsState(
                false,
                null,
                new List<IdeSolutionEntry>
                {
                    new(solutionId, "./RepoOne.sln", "./RepoOne.sln", "Repo One",
                        Guid.Parse("8c80d1a5-5d2b-4a9e-b0e1-5d9733a1cb5d"), "./RepoOne.sln (Repo One)")
                },
                solutionId);
            var treeState = new IdeTreeState(new Dictionary<string, IdeTreeViewState>(StringComparer.OrdinalIgnoreCase)
            {
                [solutionId] = new(false, true, null, new List<TreeItemData<CodeTreeEntryModel>>
                {
                    new()
                    {
                        Value = new CodeTreeEntryModel("./RepoOne", "Repo One", "Repo One", CodeTreeEntryType.Project,
                            "alpha", "Repo One", null, null, null, null, null, null),
                        Text = "Repo One"
                    }
                })
            });

            context.Services.AddScoped<IState<IdeSolutionsState>>(_ =>
                new StateWrapper<IdeSolutionsState>(solutionsState));
            context.Services.AddScoped<IState<IdeTreeState>>(_ => new StateWrapper<IdeTreeState>(treeState));
            context.Services.AddScoped<IState<IdeTreeSettingsState>>(_ =>
                new StateWrapper<IdeTreeSettingsState>(IdeTreeSettingsState.Default));
            context.Services.AddScoped<IState<IdeDecisionsState>>(_ =>
                new StateWrapper<IdeDecisionsState>(new IdeDecisionsState()));
            context.Services.AddScoped<IState<IdeSymbolsState>>(_ =>
                new StateWrapper<IdeSymbolsState>(new IdeSymbolsState()));
            context.Services.AddScoped<IDispatcher>(_ => new RecordingDispatcher());
            context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

            context.RenderComponent<MudPopoverProvider>();
            var cut = context.RenderComponent<IdeTree>();

            cut.WaitForAssertion(() => Assert.Contains("Repo One", cut.Markup));
        }

        [Fact]
        public void Ide_OpensFileTab_WhenFileSelected()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddMudServices();
            context.Services.AddLogging();
            var solutionId = "solution-1";
            var solutionsState = new IdeSolutionsState(
                false,
                null,
                new List<IdeSolutionEntry>
                {
                    new(solutionId, "./RepoOne.sln", "./RepoOne.sln", "Repo One",
                        Guid.Parse("8c80d1a5-5d2b-4a9e-b0e1-5d9733a1cb5d"), "./RepoOne.sln (Repo One)")
                },
                solutionId);
            var tab = new IdeOpenFileTab("./Program.cs", "Repo One/Program.cs", "Program.cs", "  class Program {}")
            {
                LastCommitAuthor = "alice",
                LastCommitAuthorEmail = "alice@example.com",
                LastCommitDateUtc = DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
                AbbreviatedSha = "sha1",
                LastCommitSubject = "Update"
            };
            var tabsState = new IdeTabsState(new Dictionary<string, IdeTabsViewState>(StringComparer.OrdinalIgnoreCase)
            {
                [solutionId] = new(new List<IdeOpenFileTab> { tab }, 0, null)
            });

            context.Services.AddScoped<IState<IdeSolutionsState>>(_ =>
                new StateWrapper<IdeSolutionsState>(solutionsState));
            context.Services.AddScoped<IState<IdeTabsState>>(_ => new StateWrapper<IdeTabsState>(tabsState));
            context.Services.AddScoped<IState<IdeDecisionsState>>(_ =>
                new StateWrapper<IdeDecisionsState>(new IdeDecisionsState()));
            context.Services.AddScoped<IState<IdeSymbolsState>>(_ =>
                new StateWrapper<IdeSymbolsState>(new IdeSymbolsState()));
            context.Services.AddScoped<IState<IdeLayoutState>>(_ =>
                new StateWrapper<IdeLayoutState>(new IdeLayoutState()));
            context.Services.AddScoped<IDispatcher>(_ => new RecordingDispatcher());
            context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

            context.RenderComponent<MudPopoverProvider>();
            var cut = context.RenderComponent<IdeTabs>();

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("sha1", cut.Markup);
                Assert.Contains("alice", cut.Markup);
                Assert.Contains("alice@example.com", cut.Markup);
                Assert.Contains("2024-01-01 00:00:00", cut.Markup);
            });
            cut.WaitForAssertion(() =>
            {
                var lineSpan = cut.Find("span[style*='white-space: pre']");
                Assert.StartsWith("  class Program {}", lineSpan.TextContent);
            });
        }
    }
}