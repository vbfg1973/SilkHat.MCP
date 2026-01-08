using System.Reflection;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using SilkHat.Ui.Layout;
using SilkHat.Ui.Models;
using SilkHat.Ui.Pages;
using SilkHat.Ui.Services;
using SilkHat.Ui.Tests.TestHelpers;

namespace SilkHat.Ui.Tests.Pages;

public sealed class IdeTests
{
    [Fact]
    public void Ide_RendersTreeEntries_AfterRepositorySelection()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddLogging();
        context.Services.AddScoped<ThemeService>();
        context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("light");
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

        var configId = Guid.Parse("8c80d1a5-5d2b-4a9e-b0e1-5d9733a1cb5d");
        var solutionId = "solution-1";
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
          ""solutionId"": ""{solutionId}""
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
        handler.AddJsonResponse($"api/repositories/{configId}/code/solutions/{solutionId}/tree", """
{
  "items": [
    {
      "repositoryPath": "./RepoOne",
      "displayPath": "Repo One",
      "name": "Repo One",
      "type": 0,
      "projectKey": "alpha",
      "projectName": "Repo One"
    }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 1
}
""");

        context.Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        context.Services.AddScoped<RepositoryApiClient>();

        var cut = context.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<Ide>(0);
                    builder.CloseComponent();
                })));

        var stateField = typeof(Ide).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(stateField);

        var state = stateField!.GetValue(cut.FindComponent<Ide>().Instance);
        var treeField = state!.GetType().GetProperty("TreeItems");
        Assert.NotNull(treeField);

        var nodes = (System.Collections.IList)treeField!.GetValue(state)!;
        Assert.Single(nodes.Cast<object>());
        cut.WaitForAssertion(() => Assert.Contains("Repo One", cut.Markup));
    }

    [Fact]
    public async Task Ide_OpensFileTab_WhenFileSelected()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddLogging();
        context.Services.AddScoped<ThemeService>();
        context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("light");
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

        var configId = Guid.Parse("8c80d1a5-5d2b-4a9e-b0e1-5d9733a1cb5d");
        var solutionId = "solution-1";
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
          ""solutionId"": ""{solutionId}""
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
        handler.AddJsonResponse($"api/repositories/{configId}/code/solutions/{solutionId}/tree", """
{
  "items": [
    {
      "repositoryPath": "./Program.cs",
      "displayPath": "Repo One/Program.cs",
      "name": "Program.cs",
      "type": 2,
      "projectKey": "alpha",
      "projectName": "Repo One"
    }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 1
}
""");
        handler.AddJsonResponse(
            $"api/repositories/{configId}/code/solutions/{solutionId}/files?path=Repo%20One%2FProgram.cs",
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
        context.Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        context.Services.AddScoped<RepositoryApiClient>();

        var cut = context.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<Ide>(0);
                    builder.CloseComponent();
                })));

        var ide = cut.FindComponent<Ide>().Instance;
        var method = typeof(Ide).GetMethod("OnTreeEntrySelected", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var stateField = typeof(Ide).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(stateField);
        cut.WaitForAssertion(() => Assert.NotNull(stateField!.GetValue(ide)));
        var entry = new CodeTreeEntryModel(
            "./Program.cs",
            "Repo One/Program.cs",
            "Program.cs",
            CodeTreeEntryType.File,
            "alpha",
            "Repo One");

        await cut.InvokeAsync(async () =>
        {
            var task = (Task)method!.Invoke(ide, new object?[] { entry })!;
            await task;
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("sha1", cut.Markup);
            Assert.Contains("alice", cut.Markup);
            Assert.Contains("alice@example.com", cut.Markup);
            Assert.Contains("2024-01-01 00:00:00", cut.Markup);
        });

        var state = stateField!.GetValue(ide);
        var tabsField = state!.GetType().GetProperty("OpenFiles");
        Assert.NotNull(tabsField);
        cut.WaitForAssertion(() =>
        {
            var tabs = (System.Collections.IList)tabsField!.GetValue(state)!;
            Assert.Single(tabs);
        });

        var tabs = (System.Collections.IList)tabsField!.GetValue(state)!;
        var tab = tabs[0]!;
        var contentProperty = tab.GetType().GetProperty("Content");
        Assert.NotNull(contentProperty);
        Assert.Equal("class Program {}", contentProperty!.GetValue(tab));
    }
}
