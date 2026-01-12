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

namespace SilkHat.Ui.Tests.Components
{
    public sealed class IdeCallStackPopupTests
    {
        [Fact]
        public async Task RendersCallStackNodesFromState()
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
                    new(solutionId, "./RepoOne.sln", "./RepoOne.sln", "Repo One", Guid.NewGuid(),
                        "./RepoOne.sln (Repo One)")
                },
                solutionId);

            var nodes = new List<MethodCallStackNodeModel>
            {
                new(
                    "0_Test.Service.DoWork.none_0",
                    0,
                    0,
                    "Test",
                    "Entry",
                    "Run",
                    Array.Empty<string>(),
                    "Test.Entry.Run.none",
                    "Test",
                    "Service",
                    "DoWork",
                    Array.Empty<string>(),
                    "Test.Service.DoWork.none",
                    null,
                    new CodeLocationModel(
                        "./Sample.cs",
                        new CodeTextSpanModel(10, 5),
                        new CodeLineSpanModel(4, 1, 4, 5)),
                    null,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    true,
                    Array.Empty<string>(),
                    Array.Empty<string?>())
            };

            var callStackState = new IdeCallStackState(
                new Dictionary<string, IdeCallStackViewState>(StringComparer.OrdinalIgnoreCase)
                {
                    [solutionId] = new(
                        true,
                        false,
                        false,
                        null,
                        null,
                        null,
                        "symbol-key",
                        false,
                        nodes,
                        "sequenceDiagram",
                        IdeCallStackShadingMode.None,
                        null)
                });

            context.Services.AddScoped<IState<IdeSolutionsState>>(_ =>
                new StateWrapper<IdeSolutionsState>(solutionsState));
            context.Services.AddScoped<IState<IdeCallStackState>>(_ =>
                new StateWrapper<IdeCallStackState>(callStackState));
            context.Services.AddScoped<IDispatcher>(_ => new RecordingDispatcher());
            context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

            context.RenderComponent<MudPopoverProvider>();
            var provider = context.RenderComponent<MudDialogProvider>();
            var dialogService = context.Services.GetRequiredService<IDialogService>();

            await dialogService.ShowAsync<IdeCallStackPopup>("Method Call Stack");

            provider.WaitForAssertion(() =>
            {
                Assert.Contains("Service.DoWork", provider.Markup);
                Assert.Contains("Decision required", provider.Markup);
            });
        }
    }
}