using System.Text.Json;
using Bunit;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using SilkHat.Ui.Components.Ide.Decisions;
using SilkHat.Ui.Models;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Models;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.Components;

public sealed class DecisionToolboxTests
{
    [Fact]
    public void DecisionToolboxContent_RendersPendingDecision()
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
                new(solutionId, "./RepoOne.sln", "./RepoOne.sln", "Repo One", Guid.NewGuid(), "./RepoOne.sln (Repo One)")
            },
            solutionId);

        var payload = new ResolveInterfaceDecisionPayloadModel(
            "Samples.IGreetingProvider",
            "T:Samples.IGreetingProvider",
            "M:Samples.IGreetingProvider.GetGreeting(System.String)",
            new List<ResolveInterfaceDecisionCandidateModel>
            {
                new("Samples.FriendlyGreetingProvider", "T:Samples.FriendlyGreetingProvider", "M:Samples.FriendlyGreetingProvider.GetGreeting(System.String)")
            },
            null,
            null);

        var payloadJson = JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var pending = new List<DecisionSummaryModel>
        {
            new(
                Guid.NewGuid(),
                DecisionTypeModel.ResolveInterface,
                DecisionStatusModel.Pending,
                false,
                true,
                "IGreetingProvider",
                payload.InterfaceMethodDocId,
                DateTimeOffset.UtcNow,
                null,
                null,
                payloadJson)
        };

        var decisionsState = new IdeDecisionsState(new Dictionary<string, IdeDecisionsViewState>(StringComparer.OrdinalIgnoreCase)
        {
            [solutionId] = IdeDecisionsViewState.Default with { Pending = pending }
        });

        context.Services.AddScoped<IState<IdeSolutionsState>>(_ => new StateWrapper<IdeSolutionsState>(solutions));
        context.Services.AddScoped<IState<IdeDecisionsState>>(_ => new StateWrapper<IdeDecisionsState>(decisionsState));
        context.Services.AddScoped<IDispatcher>(_ => new RecordingDispatcher());
        context.Services.AddScoped<IActionSubscriber>(_ => new NoOpActionSubscriber());

        context.RenderComponent<MudBlazor.MudPopoverProvider>();
        var component = context.RenderComponent<DecisionToolboxContent>();

        Assert.Contains("Pending Decisions", component.Markup);
        Assert.Contains("IGreetingProvider", component.Markup);
    }

    [Fact]
    public void ResolveInterfaceDecisionControl_DispatchesResolveAction()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();

        var dispatcher = new RecordingDispatcher();
        context.Services.AddScoped<IDispatcher>(_ => dispatcher);

        var decision = new DecisionSummaryModel(
            Guid.NewGuid(),
            DecisionTypeModel.ResolveInterface,
            DecisionStatusModel.Pending,
            false,
            true,
            "IGreetingProvider",
            "M:Samples.IGreetingProvider.GetGreeting(System.String)",
            DateTimeOffset.UtcNow,
            null,
            null,
            null);

        var payload = new ResolveInterfaceDecisionPayloadModel(
            "Samples.IGreetingProvider",
            "T:Samples.IGreetingProvider",
            "M:Samples.IGreetingProvider.GetGreeting(System.String)",
            new List<ResolveInterfaceDecisionCandidateModel>
            {
                new("Samples.FriendlyGreetingProvider", "T:Samples.FriendlyGreetingProvider", "M:Samples.FriendlyGreetingProvider.GetGreeting(System.String)")
            },
            "T:Samples.FriendlyGreetingProvider",
            "M:Samples.FriendlyGreetingProvider.GetGreeting(System.String)");

        context.RenderComponent<MudBlazor.MudPopoverProvider>();
        var component = context.RenderComponent<ResolveInterfaceDecisionControl>(parameters => parameters
            .Add(p => p.Decision, decision)
            .Add(p => p.Payload, payload)
            .Add(p => p.SolutionId, "solution-1"));

        var saveButton = component.FindAll("button")
            .First(button => button.TextContent.Contains("Save", StringComparison.OrdinalIgnoreCase));
        saveButton.Click();

        var action = Assert.IsType<ResolveDecisionAction>(Assert.Single(dispatcher.Actions));
        Assert.Equal(decision.Id, action.Request.DecisionId);
    }

    [Fact]
    public async Task DecisionNotesDialog_DispatchesUpdateNotesAction()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();

        var dispatcher = new RecordingDispatcher();
        context.Services.AddScoped<IDispatcher>(_ => dispatcher);

        var provider = context.RenderComponent<MudBlazor.MudDialogProvider>();
        var dialogService = context.Services.GetRequiredService<MudBlazor.IDialogService>();

        var decisionId = Guid.NewGuid();
        var parameters = new MudBlazor.DialogParameters
        {
            ["DecisionId"] = decisionId,
            ["SolutionId"] = "solution-1",
            ["Notes"] = "Initial notes"
        };

        await dialogService.ShowAsync<DecisionNotesDialog>("Decision Notes", parameters);

        provider.WaitForAssertion(() => Assert.Contains("Notes", provider.Markup));

        var saveButton = provider.FindAll("button")
            .First(button => button.TextContent.Contains("Save", StringComparison.OrdinalIgnoreCase));
        saveButton.Click();

        var action = Assert.IsType<UpdateDecisionNotesAction>(Assert.Single(dispatcher.Actions));
        Assert.Equal(decisionId, action.Request.DecisionId);
        Assert.Equal("Initial notes", action.Request.Notes);
    }
}
