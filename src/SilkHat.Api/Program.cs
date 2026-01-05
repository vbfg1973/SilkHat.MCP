using Microsoft.EntityFrameworkCore;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Services;
using SilkHat.Analysis.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Services;
using SilkHat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("SilkHat") ?? "Data Source=./silkhat.db";
builder.Services.AddDbContext<SilkHatDbContext>(options => options.UseSqlite(connectionString));
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

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SilkHatDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

app.Run();
