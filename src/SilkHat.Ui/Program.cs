using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fluxor;
using MudBlazor.Services;
using Serilog;
using SilkHat.Ui;
using SilkHat.Ui.State;
using SilkHat.Ui.Services.Http;
using SilkHat.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

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
builder.Services.AddScoped<VisualizationThemeService>();
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
    options.AddMiddleware<FluxorLoggingMiddleware>();
});

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

builder.Services.AddScoped<CorrelationIdHandler>();
builder.Services.AddHttpClient<RepositoryApiClient>(client =>
    {
        client.BaseAddress = baseAddress;
    })
    .AddHttpMessageHandler<CorrelationIdHandler>();

await builder.Build().RunAsync();
