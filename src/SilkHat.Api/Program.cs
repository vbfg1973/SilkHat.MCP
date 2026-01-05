using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Extensions;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("SilkHat") ?? "Data Source=./silkhat.db";
builder.Services.AddDbContext<SilkHatDbContext>(options => options.UseSqlite(connectionString));

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

app.MapGet("/api/health", () => Results.Ok("OK"))
    .WithName("Health");

app.MapGet("/api/repositories", async (SilkHatDbContext dbContext) =>
{
    var configs = await dbContext.RepositoryConfigs
        .AsNoTracking()
        .OrderBy(config => config.Name)
        .Select(config => config.ToDto())
        .ToListAsync();

    return Results.Ok(configs);
});

app.MapGet("/api/repositories/{id:guid}", async (Guid id, SilkHatDbContext dbContext, HttpContext context) =>
{
    var config = await dbContext.RepositoryConfigs
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);

    if (config is null)
    {
        return Problem(context, StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
    }

    return Results.Ok(config.ToDto());
});

app.MapPost("/api/repositories", async (CreateRepositoryConfigRequest request, SilkHatDbContext dbContext, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
    }

    if (string.IsNullOrWhiteSpace(request.RootPath))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "RootPath is required.", "Validation");
    }

    if (request.GroupId.HasValue)
    {
        var groupExists = await dbContext.RepositoryGroups.AnyAsync(group => group.Id == request.GroupId);
        if (!groupExists)
        {
            return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "GroupId does not match an existing group.", "Validation");
        }
    }

    if (!TryNormalizeRootPath(request.RootPath, out var normalizedPath, out var error))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", error ?? "RootPath is invalid.", "Validation");
    }

    var config = new RepositoryConfig
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        RootPath = normalizedPath,
        Description = NormalizeOptional(request.Description),
        GroupId = request.GroupId
    };

    dbContext.RepositoryConfigs.Add(config);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/repositories/{config.Id}", config.ToDto());
});

app.MapPut("/api/repositories/{id:guid}", async (Guid id, UpdateRepositoryConfigRequest request, SilkHatDbContext dbContext, HttpContext context) =>
{
    var config = await dbContext.RepositoryConfigs.FirstOrDefaultAsync(item => item.Id == id);
    if (config is null)
    {
        return Problem(context, StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
    }

    if (string.IsNullOrWhiteSpace(request.RootPath))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "RootPath is required.", "Validation");
    }

    if (request.GroupId.HasValue)
    {
        var groupExists = await dbContext.RepositoryGroups.AnyAsync(group => group.Id == request.GroupId);
        if (!groupExists)
        {
            return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "GroupId does not match an existing group.", "Validation");
        }
    }

    if (!TryNormalizeRootPath(request.RootPath, out var normalizedPath, out var error))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", error ?? "RootPath is invalid.", "Validation");
    }

    config.Name = request.Name.Trim();
    config.RootPath = normalizedPath;
    config.Description = NormalizeOptional(request.Description);
    config.GroupId = request.GroupId;

    await dbContext.SaveChangesAsync();

    return Results.Ok(config.ToDto());
});

app.MapGet("/api/repository-groups", async (SilkHatDbContext dbContext) =>
{
    var groups = await dbContext.RepositoryGroups
        .AsNoTracking()
        .OrderBy(group => group.Name)
        .Select(group => group.ToDto())
        .ToListAsync();

    return Results.Ok(groups);
});

app.MapGet("/api/repository-groups/{id:guid}", async (Guid id, SilkHatDbContext dbContext, HttpContext context) =>
{
    var group = await dbContext.RepositoryGroups
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);

    if (group is null)
    {
        return Problem(context, StatusCodes.Status404NotFound, "Not Found", "Repository group not found.", "NotFound");
    }

    return Results.Ok(group.ToDto());
});

app.MapPost("/api/repository-groups", async (CreateRepositoryGroupRequest request, SilkHatDbContext dbContext, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
    }

    var group = new RepositoryGroup
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Description = NormalizeOptional(request.Description)
    };

    dbContext.RepositoryGroups.Add(group);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/repository-groups/{group.Id}", group.ToDto());
});

app.MapPut("/api/repository-groups/{id:guid}", async (Guid id, UpdateRepositoryGroupRequest request, SilkHatDbContext dbContext, HttpContext context) =>
{
    var group = await dbContext.RepositoryGroups.FirstOrDefaultAsync(item => item.Id == id);
    if (group is null)
    {
        return Problem(context, StatusCodes.Status404NotFound, "Not Found", "Repository group not found.", "NotFound");
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Problem(context, StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
    }

    group.Name = request.Name.Trim();
    group.Description = NormalizeOptional(request.Description);

    await dbContext.SaveChangesAsync();

    return Results.Ok(group.ToDto());
});

app.Run();

static IResult Problem(HttpContext context, int statusCode, string title, string detail, string category)
{
    var correlationId = context.Items.TryGetValue("CorrelationId", out var value)
        ? value?.ToString()
        : null;

    return Results.Problem(
        title: title,
        detail: detail,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["category"] = category
        });
}

static bool TryNormalizeRootPath(string rootPath, out string normalizedPath, out string? error)
{
    try
    {
        normalizedPath = Path.GetFullPath(rootPath.Trim());
        error = null;
        return true;
    }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
    {
        normalizedPath = string.Empty;
        error = ex.Message;
        return false;
    }
}

static string? NormalizeOptional(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
