using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Extensions;
using SilkHat.Analysis.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;
using System.Linq;
using SilkHat.Api.Services;

namespace SilkHat.Api.Controllers;

[Route("api/repositories")]
public sealed class RepositoryConfigsController : ApiControllerBase
{
    private readonly SilkHatDbContext _dbContext;
    private readonly ILoadedRepositoryStore _store;
    private readonly IRepositoryDiscoveryService _discovery;
    private readonly IApiCache _cache;
    private readonly SolutionIdentityResolver _solutionIdentityResolver = new();

    public RepositoryConfigsController(
        SilkHatDbContext dbContext,
        ILoadedRepositoryStore store,
        IRepositoryDiscoveryService discovery,
        IApiCache cache)
    {
        _dbContext = dbContext;
        _store = store;
        _discovery = discovery;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RepositoryConfigDto>>> GetAll(
        [FromQuery] PagingQuery pagingQuery,
        CancellationToken cancellationToken)
    {
        var paging = pagingQuery.ResolvePaging();
        var configsQuery = _dbContext.RepositoryConfigs
            .Include(config => config.Solutions)
            .AsNoTracking()
            .OrderBy(config => config.Name)
            .Select(config => config.ToDto());

        return Ok(await configsQuery.ToPagedResultAsync(paging, cancellationToken));
    }

    [HttpGet("loaded")]
    public async Task<ActionResult<PagedResult<RepositoryConfigDto>>> GetLoaded(
        [FromQuery] PagingQuery pagingQuery,
        CancellationToken cancellationToken)
    {
        var paging = pagingQuery.ResolvePaging();
        var cacheKey = $"repositories:loaded:{paging.PageNumber}:{paging.PageSize}";
        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var loadedIds = _store.GetAll().Select(repo => repo.ConfigId).ToHashSet();
                var configsQuery = _dbContext.RepositoryConfigs
                    .Include(config => config.Solutions)
                    .AsNoTracking()
                    .Where(config => loadedIds.Contains(config.Id))
                    .OrderBy(config => config.Name)
                    .Select(config => config.ToDto());

                return await configsQuery.ToPagedResultAsync(paging, token);
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RepositoryConfigDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var config = await _dbContext.RepositoryConfigs
            .Include(item => item.Solutions)
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

        if (!TryValidateSolutions(request.Solutions, out var solutionError))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", solutionError ?? "Solutions are required.", "Validation");
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
        config.Solutions = BuildSolutions(config, request.Solutions);
        _dbContext.RepositorySolutionConfigs.AddRange(config.Solutions);

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
        var config = await _dbContext.RepositoryConfigs
            .Include(item => item.Solutions)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

        if (!TryValidateSolutions(request.Solutions, out var solutionError))
        {
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", solutionError ?? "Solutions are required.", "Validation");
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
        _dbContext.RepositorySolutionConfigs.RemoveRange(config.Solutions);
        config.Solutions.Clear();
        var updatedSolutions = BuildSolutions(config, request.Solutions);
        foreach (var solution in updatedSolutions)
        {
            config.Solutions.Add(solution);
        }
        _dbContext.RepositorySolutionConfigs.AddRange(updatedSolutions);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(config.ToDto());
    }

    private static bool TryValidateSolutions(IReadOnlyList<RepositorySolutionDto>? solutions, out string? error)
    {
        error = null;

        if (solutions is null || solutions.Count == 0)
        {
            error = "At least one solution is required.";
            return false;
        }

        if (!solutions.Any(solution => solution.IsEnabled))
        {
            error = "At least one solution must be enabled.";
            return false;
        }

        foreach (var solution in solutions)
        {
            if (string.IsNullOrWhiteSpace(solution.RelativePath))
            {
                error = "Solution path is required.";
                return false;
            }
        }

        return true;
    }

    private List<RepositorySolutionConfig> BuildSolutions(RepositoryConfig config, IReadOnlyList<RepositorySolutionDto>? solutions)
    {
        var results = new List<RepositorySolutionConfig>();
        if (solutions is null)
        {
            return results;
        }

        foreach (var solution in solutions
                     .GroupBy(solution => NormalizeSolutionPath(solution.RelativePath), StringComparer.OrdinalIgnoreCase))
        {
            var suppliedId = solution
                .Select(entry => entry.SolutionId)
                .FirstOrDefault(entry => !string.IsNullOrWhiteSpace(entry));
            var solutionId = !string.IsNullOrWhiteSpace(suppliedId)
                ? suppliedId!
                : _solutionIdentityResolver.ResolveFromRelativePath(config.RootPath, solution.Key);

            results.Add(new RepositorySolutionConfig
            {
                Id = Guid.NewGuid(),
                RepositoryConfigId = config.Id,
                RepositoryConfig = config,
                RelativePath = solution.Key,
                SolutionId = solutionId,
                IsEnabled = solution.Any(entry => entry.IsEnabled)
            });
        }

        return results;
    }

    private static string NormalizeSolutionPath(string path)
    {
        var trimmed = path.Trim().Replace('\\', '/').TrimStart('/');
        if (trimmed.StartsWith("./", StringComparison.Ordinal))
        {
            return "./" + trimmed[2..];
        }

        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : "./" + trimmed;
    }
}
