using Microsoft.EntityFrameworkCore;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
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
