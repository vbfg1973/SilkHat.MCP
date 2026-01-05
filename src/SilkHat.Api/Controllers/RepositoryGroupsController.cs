using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Extensions;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Commands;
using SilkHat.Analysis.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;
using System.Linq;

namespace SilkHat.Api.Controllers;

[Route("api/repository-groups")]
public sealed class RepositoryGroupsController : ApiControllerBase
{
    private readonly SilkHatDbContext _dbContext;
    private readonly ILoadedRepositoryStore _store;
    private readonly ICodeWorkspaceStore _codeStore;
    private readonly IRepoCommandProcessor _processor;
    private readonly ICodeWorkspaceLoader _workspaceLoader;

    public RepositoryGroupsController(
        SilkHatDbContext dbContext,
        ILoadedRepositoryStore store,
        ICodeWorkspaceStore codeStore,
        IRepoCommandProcessor processor,
        ICodeWorkspaceLoader workspaceLoader)
    {
        _dbContext = dbContext;
        _store = store;
        _codeStore = codeStore;
        _processor = processor;
        _workspaceLoader = workspaceLoader;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepositoryGroupDto>>> GetAll(CancellationToken cancellationToken)
    {
        var groups = await _dbContext.RepositoryGroups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .Select(group => group.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(groups);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RepositoryGroupDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await _dbContext.RepositoryGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (group is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository group not found.", "NotFound");
        }

        return Ok(group.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<RepositoryGroupDto>> Create(
        [FromBody] CreateRepositoryGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
        }

        var group = new RepositoryGroup
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = RepositoryInputNormalization.NormalizeOptional(request.Description)
        };

        _dbContext.RepositoryGroups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RepositoryGroupDto>> Update(
        Guid id,
        [FromBody] UpdateRepositoryGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.RepositoryGroups.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (group is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository group not found.", "NotFound");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
        }

        group.Name = request.Name.Trim();
        group.Description = RepositoryInputNormalization.NormalizeOptional(request.Description);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(group.ToDto());
    }

    [HttpPost("{id:guid}/load")]
    public async Task<ActionResult<RepositoryGroupLoadResultDto>> LoadGroup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.RepositoryGroups
            .Include(group => group.RepositoryConfigs)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (group is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository group not found.", "NotFound");
        }

        var results = new List<RepositoryLoadResultDto>();
        foreach (var config in group.RepositoryConfigs.OrderBy(config => config.Name))
        {
            var context = new RepoCommandContext(config.Id, config.RootPath, _store, _codeStore, _workspaceLoader);
            var command = new LoadRepositoryCommand();
            RepoEventDto? lastEvent = null;

            await foreach (var evt in _processor.ExecuteAsync(command, context, cancellationToken))
            {
                lastEvent = evt;
            }

            var loaded = _store.Get(config.Id) is not null;
            var message = lastEvent?.Summary?.Message ?? lastEvent?.Message ?? "No events returned.";
            results.Add(new RepositoryLoadResultDto(config.Id, config.Name, loaded, message));
        }

        return Ok(new RepositoryGroupLoadResultDto(id, results));
    }
}
