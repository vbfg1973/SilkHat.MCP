using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Commands;
using SilkHat.Analysis.Models;
using SilkHat.Analysis.Services;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;

namespace SilkHat.Api.Controllers;

[Route("api/repositories")]
public sealed class RepositoryLoadController : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SilkHatDbContext _dbContext;
    private readonly LoadedRepositoryStore _store;
    private readonly IRepoCommandProcessor _processor;

    public RepositoryLoadController(
        SilkHatDbContext dbContext,
        LoadedRepositoryStore store,
        IRepoCommandProcessor processor)
    {
        _dbContext = dbContext;
        _store = store;
        _processor = processor;
    }

    [HttpPost("{id:guid}/load")]
    [Produces("application/x-ndjson")]
    public async Task<IActionResult> Load(Guid id, CancellationToken cancellationToken)
    {
        var config = await _dbContext.RepositoryConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (config is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";

        var context = new RepoCommandContext(config.Id, config.RootPath, _store);
        var command = new LoadRepositoryCommand();

        await foreach (var evt in _processor.ExecuteAsync(command, context, cancellationToken))
        {
            var json = JsonSerializer.Serialize(evt, JsonOptions);
            await Response.WriteAsync(json + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpPost("{id:guid}/unload")]
    public async Task<ActionResult<RepoEventDto>> Unload(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.RepositoryConfigs
            .AsNoTracking()
            .AnyAsync(item => item.Id == id, cancellationToken);

        if (!exists)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
        }

        var unloaded = _store.Unload(id);
        var message = unloaded ? "Repository unloaded." : "Repository was not loaded.";

        return Ok(new RepoEventDto(
            RepoEventKind.Completed,
            "unload",
            message,
            100,
            null,
            null,
            new RepoEventSummaryDto(message)));
    }
}
