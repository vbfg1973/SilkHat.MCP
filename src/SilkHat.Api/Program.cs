using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Analysis.Services;
using SilkHat.Api.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Analysis.Services.Complexity;
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
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
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
builder.Services.AddSingleton<ICodeTreeService, CodeTreeService>();
builder.Services.AddSingleton<ICodeTreeMetricsService, CodeTreeMetricsService>();
builder.Services.AddSingleton<ICodeTreeQueryService, CodeTreeQueryService>();
builder.Services.AddSingleton<ICodeTreeMetricsCacheStore, CodeTreeMetricsCacheStore>();
builder.Services.AddSingleton<ICodeTreeMetricsPrecomputeService, CodeTreeMetricsPrecomputeService>();
builder.Services.AddSingleton<IGraphStoreProvider, GraphStoreProvider>();
builder.Services.AddSingleton<IGraphQueryService, GraphQueryService>();
builder.Services.AddSingleton<IIndexingStatusStore, IndexingStatusStore>();
builder.Services.AddSingleton<ICodeFileService, CodeFileService>();
builder.Services.AddSingleton<ICodeSymbolOutlineService, CodeSymbolOutlineService>();
builder.Services.AddSingleton<ISymbolDescriptionService, SymbolDescriptionService>();
builder.Services.AddSingleton<IComplexityStrategy, CognitiveComplexityStrategy>();
builder.Services.AddSingleton<IComplexityStrategy, CyclomaticComplexityStrategy>();
builder.Services.AddSingleton<IComplexityStrategy, IndentationComplexityStrategy>();
builder.Services.AddSingleton<IComplexityStrategyFactory, ComplexityStrategyFactory>();
builder.Services.AddSingleton<IMethodComplexityService, MethodComplexityService>();
builder.Services.AddSingleton<ITypeComplexityService, TypeComplexityService>();
builder.Services.AddSingleton<IComplexityMetricsAggregator, ComplexityMetricsAggregator>();
builder.Services.AddScoped<IMethodImplementationDecisionService, MethodImplementationDecisionService>();
builder.Services.AddScoped<IDecisionService, DecisionService>();
builder.Services.AddScoped<IMethodCallStackService, MethodCallStackService>();
builder.Services.AddSingleton<IMethodCallStackMermaidService, MethodCallStackMermaidService>();
builder.Services.Configure<RepositoryDiscoveryOptions>(options =>
{
    options.RepoRoot = builder.Configuration["REPO_ROOT"];
});
builder.Services.AddSingleton<IRepositoryDiscoveryService, RepositoryDiscoveryService>();
builder.Services.AddSingleton<IGitCommandRunner, GitCommandRunner>();
builder.Services.AddSingleton<IGitRepositoryCacheStore, GitRepositoryCacheStore>();
builder.Services.AddSingleton<IGitCli, GitCli>();
builder.Services.AddSingleton<IGitMetricsAggregator, GitMetricsAggregator>();
builder.Services.AddHybridCache();
builder.Services.AddSingleton<IApiCache, ApiCache>();

var app = builder.Build();

app.UseCors("UiCors");
app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString("N");

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