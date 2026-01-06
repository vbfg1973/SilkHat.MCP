using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Serilog;
using SilkHat.Ui;
using SilkHat.Ui.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<RepositoryApiClient>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var baseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var absolute)
        ? absolute
        : new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBaseUrl);

if (baseAddress.Scheme == Uri.UriSchemeFile)
{
    throw new InvalidOperationException($"API base address resolved to file scheme: {baseAddress}");
}

Log.Logger.Information("Resolved API base address: {BaseAddress}", baseAddress);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = baseAddress });

await builder.Build().RunAsync();
