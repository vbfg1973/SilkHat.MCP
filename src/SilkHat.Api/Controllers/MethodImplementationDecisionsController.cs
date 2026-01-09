using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Controllers;

[Route("api/repositories/{id:guid}/code/solutions/{solutionId}/decisions/interface-methods")]
public sealed class MethodImplementationDecisionsController : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SilkHatDbContext _dbContext;

    public MethodImplementationDecisionsController(SilkHatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MethodImplementationDecisionDto>>> GetDecisions(
        Guid id,
        string solutionId,
        CancellationToken cancellationToken)
    {
        var decisions = await _dbContext.MethodImplementationDecisions
            .AsNoTracking()
            .Where(decision => decision.RepositoryConfigId == id && decision.SolutionId == solutionId)
            .OrderBy(decision => decision.InterfaceTypeName)
            .ThenBy(decision => decision.InterfaceMethodSignature)
            .Select(decision => new MethodImplementationDecisionDto(
                decision.Id,
                decision.InterfaceTypeName,
                decision.InterfaceTypeDocumentationId,
                decision.InterfaceMethodSignature,
                decision.InterfaceMethodDocumentationId,
                decision.ImplementationTypeName,
                decision.ImplementationTypeDocumentationId,
                decision.ImplementationMethodDocumentationId,
                decision.CreatedUtc,
                decision.UpdatedUtc))
            .ToListAsync(cancellationToken);

        return Ok(decisions);
    }

    [HttpPost]
    public async Task<ActionResult<MethodImplementationDecisionDto>> CreateDecision(
        Guid id,
        string solutionId,
        [FromBody] MethodImplementationDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();
            var message = errors.Count > 0 ? string.Join(" ", errors) : "Validation failed.";
            return ProblemWithCategory(StatusCodes.Status400BadRequest, "Validation Failed", message, "Validation");
        }

        var existing = await _dbContext.MethodImplementationDecisions
            .FirstOrDefaultAsync(decision =>
                decision.RepositoryConfigId == id
                && decision.SolutionId == solutionId
                && (!string.IsNullOrWhiteSpace(request.InterfaceMethodDocumentationId)
                    ? decision.InterfaceMethodDocumentationId == request.InterfaceMethodDocumentationId
                    : decision.InterfaceMethodSignature == request.InterfaceMethodSignature),
                cancellationToken);

        if (existing is not null)
        {
            existing.InterfaceTypeName = request.InterfaceTypeName;
            existing.InterfaceTypeDocumentationId = request.InterfaceTypeDocumentationId;
            existing.ImplementationTypeName = request.ImplementationTypeName;
            existing.ImplementationTypeDocumentationId = request.ImplementationTypeDocumentationId;
            existing.ImplementationMethodDocumentationId = request.ImplementationMethodDocumentationId;
            existing.InterfaceMethodDocumentationId = request.InterfaceMethodDocumentationId;
        }
        else
        {
            existing = new MethodImplementationDecision
            {
                Id = Guid.NewGuid(),
                RepositoryConfigId = id,
                SolutionId = solutionId,
                InterfaceTypeName = request.InterfaceTypeName,
                InterfaceTypeDocumentationId = request.InterfaceTypeDocumentationId,
                InterfaceMethodSignature = request.InterfaceMethodSignature,
                InterfaceMethodDocumentationId = request.InterfaceMethodDocumentationId,
                ImplementationTypeName = request.ImplementationTypeName,
                ImplementationTypeDocumentationId = request.ImplementationTypeDocumentationId,
                ImplementationMethodDocumentationId = request.ImplementationMethodDocumentationId
            };
            _dbContext.MethodImplementationDecisions.Add(existing);
        }

        await UpsertUnifiedDecision(id, solutionId, existing, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new MethodImplementationDecisionDto(
            existing.Id,
            existing.InterfaceTypeName,
            existing.InterfaceTypeDocumentationId,
            existing.InterfaceMethodSignature,
            existing.InterfaceMethodDocumentationId,
            existing.ImplementationTypeName,
            existing.ImplementationTypeDocumentationId,
            existing.ImplementationMethodDocumentationId,
            existing.CreatedUtc,
            existing.UpdatedUtc);

        return Ok(dto);
    }

    private async Task UpsertUnifiedDecision(
        Guid repositoryId,
        string solutionId,
        MethodImplementationDecision decision,
        MethodImplementationDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        var subjectKey = !string.IsNullOrWhiteSpace(request.InterfaceMethodDocumentationId)
            ? request.InterfaceMethodDocumentationId
            : request.InterfaceMethodSignature;
        var payload = new ResolveInterfaceDecisionPayloadDto(
            request.InterfaceTypeName,
            request.InterfaceTypeDocumentationId ?? string.Empty,
            request.InterfaceMethodDocumentationId ?? string.Empty,
            Array.Empty<ResolveInterfaceDecisionCandidateDto>(),
            request.ImplementationTypeDocumentationId,
            request.ImplementationMethodDocumentationId);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        var unified = await _dbContext.Decisions
            .FirstOrDefaultAsync(existingDecision =>
                existingDecision.RepositoryConfigId == repositoryId
                && existingDecision.SolutionId == solutionId
                && existingDecision.DecisionType == DecisionType.ResolveInterface
                && existingDecision.SubjectKey == subjectKey,
                cancellationToken);

        if (unified is null)
        {
            unified = new Decision
            {
                Id = decision.Id,
                RepositoryConfigId = repositoryId,
                SolutionId = solutionId,
                DecisionType = DecisionType.ResolveInterface,
                Status = DecisionStatus.Resolved,
                IsActive = true,
                IsValid = true,
                Name = request.InterfaceTypeName,
                SubjectKey = subjectKey,
                DiscoveredUtc = DateTimeOffset.UtcNow,
                ResolvedUtc = DateTimeOffset.UtcNow,
                PayloadJson = payloadJson
            };
            _dbContext.Decisions.Add(unified);
        }
        else
        {
            unified.Name = request.InterfaceTypeName;
            unified.PayloadJson = payloadJson;
            unified.Status = DecisionStatus.Resolved;
            unified.IsActive = true;
            unified.IsValid = true;
            unified.ResolvedUtc = DateTimeOffset.UtcNow;
        }
    }
}
