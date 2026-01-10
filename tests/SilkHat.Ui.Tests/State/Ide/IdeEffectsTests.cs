using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Effects;
using SilkHat.Ui.State.Ide.Models;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.State.Ide;

public sealed class IdeEffectsTests
{
    [Fact]
    public async Task LoadSolutions_DispatchesSolutionsAndSelection()
    {
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler();
        handler.AddJsonResponse("api/repositories/loaded", $@"{{
  ""items"": [
    {{
      ""id"": ""{configId}"",
      ""name"": ""Repo One"",
      ""rootPath"": ""/repo"",
      ""description"": null,
      ""groupId"": null,
      ""solutions"": [
        {{
          ""relativePath"": ""./RepoOne.sln"",
          ""isEnabled"": true,
          ""solutionId"": ""solution-1""
        }}
      ],
      ""createdUtc"": ""2024-01-01T00:00:00Z"",
      ""updatedUtc"": ""2024-01-01T00:00:00Z""
    }}
  ],
  ""pageNumber"": 1,
  ""pageSize"": 50,
  ""totalCount"": 1
}}");

        var api = new RepositoryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }, NullLogger<RepositoryApiClient>.Instance);

        var dispatcher = new RecordingDispatcher();
        var effects = new IdeSolutionsEffects(api, NullLogger<IdeSolutionsEffects>.Instance);

        await effects.HandleLoadSolutions(new LoadSolutionsAction(), dispatcher);

        Assert.Contains(dispatcher.Actions, action => action is LoadSolutionsSuccessAction);
        Assert.Contains(dispatcher.Actions, action => action is SelectSolutionAction);
    }

    [Fact]
    public async Task OpenFileTab_DispatchesSuccessWithMetadata()
    {
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler();
        handler.AddJsonResponse("api/repositories/loaded", @"{ ""items"": [], ""pageNumber"": 1, ""pageSize"": 50, ""totalCount"": 0 }");
        handler.AddJsonResponse(
            $"api/repositories/{configId}/code/solutions/solution-1/files?path=Repo%20One%2FProgram.cs",
            """
{
  "repositoryPath": "./Program.cs",
  "displayPath": "Repo One/Program.cs",
  "content": "class Program {}"
}
""");
        handler.AddJsonResponse(
            $"api/repositories/{configId}/git/files/.%2FProgram.cs/last-change?includeDiff=false",
            """
{
  "path": "./Program.cs",
  "commitSha": "sha1",
  "abbreviatedSha": "sha1",
  "author": "alice",
  "authorEmail": "alice@example.com",
  "commitDateUtc": "2024-01-01T00:00:00Z",
  "subject": "Update",
  "diffLines": []
}
""");
        handler.AddJsonResponse(
            $"api/repositories/{configId}/git/files/.%2FProgram.cs/change-count",
            """
{
  "path": "./Program.cs",
  "changeCount": 4
}
""");

        var api = new RepositoryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }, NullLogger<RepositoryApiClient>.Instance);

        var dispatcher = new RecordingDispatcher();
        var solutions = new StateWrapper<IdeSolutionsState>(
            new IdeSolutionsState(false, null, Array.Empty<IdeSolutionEntry>(), null));
        var tabs = new StateWrapper<IdeTabsState>(new IdeTabsState());
        var effects = new IdeTabsEffects(api, solutions, tabs, NullLogger<IdeTabsEffects>.Instance);

        var entry = new CodeTreeEntryModel(
            "./Program.cs",
            "Repo One/Program.cs",
            "Program.cs",
            CodeTreeEntryType.File,
            "alpha",
            "Repo One",
            null,
            null,
            null,
            null,
            null,
            null);

        await effects.HandleOpenFile(new OpenFileTabAction("solution-1", configId, entry), dispatcher);

        var success = dispatcher.Actions.OfType<OpenFileTabSuccessAction>().Single();
        Assert.Equal("alice", success.Tab.LastCommitAuthor);
        Assert.Equal("sha1", success.Tab.AbbreviatedSha);
    }

    [Fact]
    public void OpenSymbolPopup_DispatchesLoadForActiveFile()
    {
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler();
        var api = new RepositoryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        }, NullLogger<RepositoryApiClient>.Instance);

        var solutions = new StateWrapper<IdeSolutionsState>(
            new IdeSolutionsState(
                false,
                null,
                new List<IdeSolutionEntry>
                {
                    new("solution-1", "./RepoOne.sln", "./RepoOne.sln", "Repo One", configId, "./RepoOne.sln (Repo One)")
                },
                "solution-1"));

        var tabs = new StateWrapper<IdeTabsState>(new IdeTabsState(new Dictionary<string, IdeTabsViewState>
        {
            ["solution-1"] = new IdeTabsViewState(
                new List<IdeOpenFileTab>
                {
                    new("./Program.cs", "Repo One/Program.cs", "Program.cs", "class Program {}")
                },
                0,
                null)
        }));

        var symbols = new StateWrapper<IdeSymbolsState>(new IdeSymbolsState());
        var dispatcher = new RecordingDispatcher();
        var effects = new IdeSymbolsEffects(api, solutions, tabs, symbols, NullLogger<IdeSymbolsEffects>.Instance);

        effects.HandleOpenPopup(new OpenSymbolPopupAction("solution-1"), dispatcher);

        var load = dispatcher.Actions.OfType<LoadFileSymbolsAction>().Single();
        Assert.Equal("./Program.cs", load.RepositoryPath);
    }
}
