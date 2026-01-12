using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using SilkHat.Ui.Layout;
using SilkHat.Ui.Services;

namespace SilkHat.Ui.Tests.Layout
{
    public sealed class MainLayoutTests
    {
        [Fact]
        public void MainLayout_RendersShell()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddMudServices();
            context.Services.AddScoped<ThemeService>();
            context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("dark");
            context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

            var cut = context.RenderComponent<MainLayout>(parameters =>
                parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Body"))));

            Assert.Contains("SilkHat", cut.Markup);
        }

        [Fact]
        public void MainLayout_TogglesDarkMode_PersistsSetting()
        {
            using var context = new TestContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddMudServices();
            context.Services.AddScoped<ThemeService>();
            context.JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("light");
            context.JSInterop.SetupVoid("localStorage.setItem", _ => true);

            var cut = context.RenderComponent<MainLayout>(parameters =>
                parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Body"))));

            var toggle = cut.Find("input[type='checkbox']");
            toggle.Change(new ChangeEventArgs { Value = true });

            cut.WaitForAssertion(() =>
            {
                var invocation = context.JSInterop.Invocations
                    .Single(call => call.Identifier == "localStorage.setItem");
                Assert.Equal("dark", invocation.Arguments[1]?.ToString());
            });
        }
    }
}