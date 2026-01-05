using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Extensions;
using SilkHat.Analysis.Abstractions;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;
using System.Linq;

namespace SilkHat.Api.Controllers;

[Route("api/repositories")]
public sealed class RepositoryConfigsController : ApiControllerBase
{
    private readonly SilkHatDbContext _dbContext;
    private readonly ILoadedRepositoryStore _store;
    private readonly IRepositoryDiscoveryService _discovery;

    public RepositoryConfigsController(
        SilkHatDbContext dbContext,
        ILoadedRepositoryStore store,
        IRepositoryDiscoveryService discovery)
    {
        _dbContext = dbContext;
        _store = store;
        _discovery = discovery;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepositoryConfigDto>>> GetAll(CancellationToken cancellationToken)
    {
        var configs = await _dbContext.RepositoryConfigs
            .AsNoTracking()
            .OrderBy(config => config.Name)
            .Select(config => config.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(configs);
    }

    [HttpGet("loaded")]
    public async Task<ActionResult<IReadOnlyList<RepositoryConfigDto>>> GetLoaded(CancellationToken cancellationToken)
    {
        var loadedIds = _store.GetAll().Select(repo => repo.ConfigId).ToHashSet();
        var configs = await _dbContext.RepositoryConfigs
            .AsNoTracking()
            .Where(config => loadedIds.Contains(config.Id))
            .OrderBy(config => config.Name)
            .Select(config => config.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(configs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RepositoryConfigDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var config = await _dbContext.RepositoryConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (config is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
        }

        return Ok(config.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<RepositoryConfigDto>> Create(
        [FromBody] CreateRepositoryConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "RootPath is required.", "Validation");
        }

        if (!RepositoryInputNormalization.TryNormalizeRootPath(request.RootPath, out var normalizedPath, out var error))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", error ?? "RootPath is invalid.", "Validation");
        }

        if (!_discovery.TryValidateRepositoryPath(normalizedPath, out var validationError))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", validationError ?? "RootPath is invalid.", "Validation");
        }

        if (request.GroupId.HasValue)
        {
            var groupExists = await _dbContext.RepositoryGroups
                .AnyAsync(group => group.Id == request.GroupId, cancellationToken);
            if (!groupExists)
            {
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "GroupId does not match an existing group.", "Validation");
            }
        }

        var config = new RepositoryConfig
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            RootPath = normalizedPath,
            Description = RepositoryInputNormalization.NormalizeOptional(request.Description),
            GroupId = request.GroupId
        };

        _dbContext.RepositoryConfigs.Add(config);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = config.Id }, config.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RepositoryConfigDto>> Update(
        Guid id,
        [FromBody] UpdateRepositoryConfigRequest request,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.RepositoryConfigs.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (config is null)
        {
            return ProblemWithCategory(StatusCodes.Status404NotFound, "Not Found", "Repository config not found.", "NotFound");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "Name is required.", "Validation");
        }

        if (string.IsNullOrWhiteSpace(request.RootPath))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "RootPath is required.", "Validation");
        }

        if (!RepositoryInputNormalization.TryNormalizeRootPath(request.RootPath, out var normalizedPath, out var error))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", error ?? "RootPath is invalid.", "Validation");
        }

        if (!_discovery.TryValidateRepositoryPath(normalizedPath, out var validationError))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", validationError ?? "RootPath is invalid.", "Validation");
        }

        if (request.GroupId.HasValue)
        {
            var groupExists = await _dbContext.RepositoryGroups
                .AnyAsync(group => group.Id == request.GroupId, cancellationToken);
            if (!groupExists)
            {
                return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", "GroupId does not match an existing group.", "Validation");
            }
        }

        config.Name = request.Name.Trim();
        config.RootPath = normalizedPath;
        config.Description = RepositoryInputNormalization.NormalizeOptional(request.Description);
        config.GroupId = request.GroupId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(config.ToDto());
    }
}
