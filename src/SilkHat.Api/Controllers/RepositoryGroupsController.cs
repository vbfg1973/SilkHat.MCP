using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Api.Extensions;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Controllers;

[Route("api/repository-groups")]
public sealed class RepositoryGroupsController : ApiControllerBase
{
    private readonly SilkHatDbContext _dbContext;

    public RepositoryGroupsController(SilkHatDbContext dbContext)
    {
        _dbContext = dbContext;
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
}
