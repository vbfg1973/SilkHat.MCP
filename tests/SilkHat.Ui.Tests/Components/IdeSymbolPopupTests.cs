using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using SilkHat.Ui.Components.Ide;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Models;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.Components;

public sealed class IdeSymbolPopupTests
{
    [Fact]
    public async Task RendersSymbolCardsFromState()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddLogging();

        var solutionId = "solution-1";
        var solutions = new IdeSolutionsState(
            false,
            null,
            new List<IdeSolutionEntry>
            {
                new(solutionId, "./RepoOne.sln", "./RepoOne.sln", Guid.NewGuid(), "./RepoOne.sln (Repo One)")
            },
            solutionId);

        var nodes = new List<CodeSymbolOutlineNodeModel>
        {
            new(
                "sym-1",
                "T:Samples.ComplexitySamples",
                "ComplexitySamples",
                "NamedType",
                "Class",
                new List<CodeSymbolOutlineNodeModel>
                {
                    new("sym-2", "M:Samples.ComplexitySamples.CalculateScore(System.Int32)", "CalculateScore", "Method", "Method", Array.Empty<CodeSymbolOutlineNodeModel>())
                })
        };

        var symbols = new IdeSymbolsState(new Dictionary<string, IdeSymbolsViewState>(StringComparer.OrdinalIgnoreCase)
        {
            [solutionId] = new IdeSymbolsViewState(true, false, null, "./AnalysisSamples.cs", nodes, null, null)
        });

        context.Services.AddScoped<IState<IdeSolutionsState>>(_ => new StateWrapper<IdeSolutionsState>(solutions));
        context.Services.AddScoped<IState<IdeSymbolsState>>(_ => new StateWrapper<IdeSymbolsState>(symbols));
        context.Services.AddScoped<IDispatcher>(_ => new RecordingDispatcher());
        context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

        var provider = context.RenderComponent<MudBlazor.MudDialogProvider>();
        var dialogService = context.Services.GetRequiredService<MudBlazor.IDialogService>();

        await dialogService.ShowAsync<IdeSymbolPopup>("File Symbols");

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("ComplexitySamples", provider.Markup);
            Assert.Contains("NamedType", provider.Markup);
            Assert.Contains("Class", provider.Markup);
            Assert.Contains("CalculateScore", provider.Markup);
        });
    }
}
