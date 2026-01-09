using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using SilkHat.Ui.Components.Ide;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.Components;

public sealed class IdeSymbolPopupCallStackTests
{
    [Fact]
    public async Task CallStackButtonDispatchesOpenAction_ForSelectedMethod()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddLogging();

        var solutionId = "solution-1";
        var nodes = new List<CodeSymbolOutlineNodeModel>
        {
            new("symbol-key", "M:Samples.Program.Run", "Run", "Method", "Method", Array.Empty<CodeSymbolOutlineNodeModel>())
        };
        var symbolsState = new IdeSymbolsState(new Dictionary<string, IdeSymbolsViewState>(StringComparer.OrdinalIgnoreCase)
        {
            [solutionId] = new IdeSymbolsViewState(true, false, null, "./Program.cs", nodes, "M:Samples.Program.Run", "symbol-key")
        });
        var solutionsState = new IdeSolutionsState(
            false,
            null,
            new List<IdeSolutionEntry>
            {
                new(solutionId, "./RepoOne.sln", "./RepoOne.sln", "Repo One", Guid.NewGuid(), "./RepoOne.sln (Repo One)")
            },
            solutionId);
        var callStackState = new IdeCallStackState();

        var dispatcher = new RecordingDispatcher();
        context.Services.AddScoped<IState<IdeSolutionsState>>(_ => new StateWrapper<IdeSolutionsState>(solutionsState));
        context.Services.AddScoped<IState<IdeSymbolsState>>(_ => new StateWrapper<IdeSymbolsState>(symbolsState));
        context.Services.AddScoped<IState<IdeCallStackState>>(_ => new StateWrapper<IdeCallStackState>(callStackState));
        context.Services.AddScoped<IDispatcher>(_ => dispatcher);
        context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

        context.RenderComponent<MudBlazor.MudPopoverProvider>();
        var provider = context.RenderComponent<MudBlazor.MudDialogProvider>();
        var dialogService = context.Services.GetRequiredService<MudBlazor.IDialogService>();

        await dialogService.ShowAsync<IdeSymbolPopup>("File Symbols");

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("Call Stack", provider.Markup);
        });

        var button = provider.FindAll("button")
            .First(element => element.TextContent.Contains("Call Stack", StringComparison.OrdinalIgnoreCase));
        button.Click();

        var action = Assert.IsType<OpenCallStackPopupAction>(dispatcher.Actions.Last());
        Assert.Equal(solutionId, action.SolutionId);
        Assert.Equal("M:Samples.Program.Run", action.DocumentationId);
        Assert.Equal("symbol-key", action.SymbolKey);
    }
}
