using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Services;
using SilkHat.Analysis.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Services;
using SilkHat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:10080" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("UiCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("SilkHat")
                      ?? builder.Configuration["DATABASE_URL"]
                      ?? "Host=localhost;Port=5432;Database=silkhat;Username=silkhat;Password=silkhat";
builder.Services.AddDbContext<SilkHatDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(18, 0)));
builder.Services.AddSingleton<ILoadedRepositoryStore, LoadedRepositoryStore>();
builder.Services.AddSingleton<IRepoCommandProcessor, RepoCommandProcessor>();
builder.Services.AddSingleton<ICodeWorkspaceStore, CodeWorkspaceStore>();
builder.Services.AddSingleton<ICodeWorkspaceLoader, CodeWorkspaceLoader>();
builder.Services.Configure<RepositoryDiscoveryOptions>(options =>
{
    options.RepoRoot = builder.Configuration["REPO_ROOT"];
});
builder.Services.AddSingleton<IRepositoryDiscoveryService, RepositoryDiscoveryService>();
builder.Services.AddSingleton<IGitCommandRunner, GitCommandRunner>();
builder.Services.AddSingleton<IGitRepositoryCacheStore, GitRepositoryCacheStore>();
builder.Services.AddSingleton<IGitCli, GitCli>();

var app = builder.Build();

app.UseCors("UiCors");
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SilkHatDbContext>();
    dbContext.Database.Migrate();
}

app.MapControllers();

app.Run();
