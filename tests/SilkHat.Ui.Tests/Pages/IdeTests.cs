using System.Reflection;
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
        handler.AddJsonResponse("api/repositories/loaded", $@"[
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
]");
        handler.AddJsonResponse($"api/repositories/{configId}/code/solutions", $@"[
  {{
    ""solutionId"": ""{solutionId}"",
    ""name"": ""RepoOne"",
    ""relativePath"": ""./RepoOne.sln""
  }}
]");
        handler.AddJsonResponse($"api/repositories/{configId}/code/solutions/{solutionId}/projects", "[]");
        handler.AddJsonResponse($"api/repositories/{configId}/code/solutions/{solutionId}/tree", """
[
  {
    "repositoryPath": "./src/Program.cs",
    "displayPath": "Repo One/src/Program.cs",
    "name": "Program.cs",
    "type": 2,
    "projectKey": "alpha",
    "projectName": "Repo One"
  }
]
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

        var ide = cut.FindComponent<Ide>();
        var changeMethod = typeof(Ide).GetMethod("OnRepositoryChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(changeMethod);
        var task = (Task)changeMethod!.Invoke(ide.Instance, new object?[] { configId })!;
        task.GetAwaiter().GetResult();

        ide.Render();

        var treeField = typeof(Ide).GetField("_treeEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(treeField);

        var entries = (List<CodeTreeEntryModel>)treeField!.GetValue(ide.Instance)!;
        Assert.Single(entries);
        cut.WaitForAssertion(() => Assert.Contains("Program.cs", cut.Markup));
    }
}
