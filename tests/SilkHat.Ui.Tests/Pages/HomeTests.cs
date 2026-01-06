using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using SilkHat.Ui.Layout;
using SilkHat.Ui.Pages;
using SilkHat.Ui.Services;
using SilkHat.Ui.Tests.TestHelpers;
using System.Linq;

namespace SilkHat.Ui.Tests.Pages;

public sealed class HomeTests
{
    [Fact]
    public void Home_RendersGroupsAndConfigs_FromApi()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddScoped<ThemeService>();
        context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("light");
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

        var handler = new FakeHttpMessageHandler();
        handler.AddJsonResponse("api/repository-groups", """
[
  {
    "id": "d2719b29-ff3a-4af2-9e47-7a6fbd1f25a1",
    "name": "Group One",
    "description": "Primary group",
    "createdUtc": "2024-01-01T00:00:00Z",
    "updatedUtc": "2024-01-01T00:00:00Z"
  }
]
""");
        handler.AddJsonResponse("api/repositories", """
[
  {
    "id": "53fcfc7a-2cd5-449e-b7be-7fdc02a5c785",
    "name": "Repo One",
    "rootPath": "/repo",
    "description": "Sample repo",
    "groupId": "d2719b29-ff3a-4af2-9e47-7a6fbd1f25a1",
    "solutions": [
      {
        "relativePath": "./RepoOne.sln",
        "isEnabled": true
      }
    ],
    "createdUtc": "2024-01-01T00:00:00Z",
    "updatedUtc": "2024-01-01T00:00:00Z"
  }
]
""");
        handler.AddJsonResponse("api/repositories/available", """
[
  {
    "name": "Repo One",
    "relativePath": "Repo One",
    "fullPath": "/repos/repo-one",
    "isGitRepository": true
  }
]
""");

        context.Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        context.Services.AddScoped<RepositoryApiClient>();

        var cut = context.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<Home>(0);
                    builder.CloseComponent();
                })));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Repository Group Loader", cut.Markup);
            Assert.Contains("Group One", cut.Markup);
        });
    }

    [Fact]
    public void Home_SelectGroup_ShowsSolutions()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddScoped<ThemeService>();
        context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("light");
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

        var handler = new FakeHttpMessageHandler();
        handler.AddJsonResponse("api/repository-groups", """
[
  {
    "id": "d2719b29-ff3a-4af2-9e47-7a6fbd1f25a1",
    "name": "Group One",
    "description": "Primary group",
    "createdUtc": "2024-01-01T00:00:00Z",
    "updatedUtc": "2024-01-01T00:00:00Z"
  }
]
""");
        handler.AddJsonResponse("api/repositories", """
[
  {
    "id": "53fcfc7a-2cd5-449e-b7be-7fdc02a5c785",
    "name": "Repo One",
    "rootPath": "/repo",
    "description": "Sample repo",
    "groupId": "d2719b29-ff3a-4af2-9e47-7a6fbd1f25a1",
    "solutions": [
      {
        "relativePath": "./RepoOne.sln",
        "isEnabled": true
      }
    ],
    "createdUtc": "2024-01-01T00:00:00Z",
    "updatedUtc": "2024-01-01T00:00:00Z"
  }
]
""");
        handler.AddJsonResponse("api/repositories/available", "[]");

        context.Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        context.Services.AddScoped<RepositoryApiClient>();

        var cut = context.RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body,
                (RenderFragment)(builder =>
                {
                    builder.OpenComponent<Home>(0);
                    builder.CloseComponent();
                })));

        var selectButtons = cut.FindAll("button").Where(button => button.TextContent.Trim() == "Select").ToList();
        Assert.NotEmpty(selectButtons);
        selectButtons[0].Click();

        cut.WaitForAssertion(() => Assert.Contains("./RepoOne.sln", cut.Markup));
    }
}
