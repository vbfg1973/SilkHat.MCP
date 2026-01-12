using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Services
{
    public sealed class DecisionService : IDecisionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly SilkHatDbContext _dbContext;
        private readonly IMethodImplementationDecisionService _implementationDecisionService;
        private readonly ILogger<DecisionService> _logger;

        public DecisionService(
            SilkHatDbContext dbContext,
            IMethodImplementationDecisionService implementationDecisionService,
            ILogger<DecisionService> logger)
        {
            _dbContext = dbContext;
            _implementationDecisionService = implementationDecisionService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<DecisionSummaryDto>> DiscoverPendingAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            DecisionType? type,
            CancellationToken cancellationToken)
        {
            await SyncLegacyDecisionsAsync(repositoryConfigId, solution.SolutionId, cancellationToken);

            var decisions = await _dbContext.Decisions
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solution.SolutionId
                                   && (!type.HasValue || decision.DecisionType == type.Value))
                .ToListAsync(cancellationToken);

            var decisionByKey = decisions.ToDictionary(
                decision => decision.SubjectKey,
                decision => decision,
                StringComparer.OrdinalIgnoreCase);

            if (type is null || type == DecisionType.ResolveInterface)
                await DiscoverResolveInterfaceDecisions(
                    workspace,
                    solution,
                    repositoryConfigId,
                    decisionByKey,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            var refreshed = await _dbContext.Decisions
                .AsNoTracking()
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solution.SolutionId
                                   && (!type.HasValue || decision.DecisionType == type.Value))
                .ToListAsync(cancellationToken);
            return refreshed.Select(decision => ToSummary(decision)).ToList();
        }

        public async Task<IReadOnlyList<DecisionSummaryDto>> GetPendingAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            DecisionType? type,
            string? sort,
            bool descending,
            CancellationToken cancellationToken)
        {
            await SyncLegacyDecisionsAsync(repositoryConfigId, solution.SolutionId, cancellationToken);
            var query = _dbContext.Decisions
                .AsNoTracking()
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solution.SolutionId
                                   && decision.Status == DecisionStatus.Pending);

            if (type.HasValue) query = query.Where(decision => decision.DecisionType == type.Value);

            query = ApplySort(query, sort, descending);

            var decisions = await query.ToListAsync(cancellationToken);
            return decisions.Select(decision => ToSummary(decision)).ToList();
        }

        public async Task<IReadOnlyList<DecisionSummaryDto>> GetResolvedAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            DecisionType? type,
            bool? active,
            string? sort,
            bool descending,
            CancellationToken cancellationToken)
        {
            await SyncLegacyDecisionsAsync(repositoryConfigId, solution.SolutionId, cancellationToken);
            var query = _dbContext.Decisions
                .AsNoTracking()
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solution.SolutionId
                                   && decision.Status == DecisionStatus.Resolved);

            if (type.HasValue) query = query.Where(decision => decision.DecisionType == type.Value);

            if (active.HasValue) query = query.Where(decision => decision.IsActive == active.Value);

            query = ApplySort(query, sort, descending);

            var decisions = await query.ToListAsync(cancellationToken);
            return decisions.Select(decision => ToSummary(decision)).ToList();
        }

        public async Task<DecisionSummaryDto?> ResolveAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            Guid decisionId,
            DecisionType type,
            object payload,
            CancellationToken cancellationToken)
        {
            var decision = await _dbContext.Decisions
                .FirstOrDefaultAsync(existing =>
                        existing.Id == decisionId
                        && existing.RepositoryConfigId == repositoryConfigId
                        && existing.SolutionId == solution.SolutionId,
                    cancellationToken);
            if (decision is null) return null;

            var payloadJson = SerializePayload(type, payload);
            if (payloadJson is null)
            {
                _logger.LogWarning("Decision resolve payload was not valid for type {DecisionType}", type);
                return null;
            }

            decision.PayloadJson = payloadJson;
            decision.DecisionType = type;
            decision.Status = DecisionStatus.Resolved;
            decision.IsActive = true;
            decision.ResolvedUtc = DateTimeOffset.UtcNow;
            decision.IsValid = ValidatePayload(type, decision.PayloadJson, solution);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToSummary(decision);
        }

        public async Task<DecisionSummaryDto?> SetActiveAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            Guid decisionId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var decision = await _dbContext.Decisions
                .FirstOrDefaultAsync(existing =>
                        existing.Id == decisionId
                        && existing.RepositoryConfigId == repositoryConfigId
                        && existing.SolutionId == solution.SolutionId,
                    cancellationToken);
            if (decision is null) return null;

            decision.IsActive = isActive;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToSummary(decision);
        }

        public async Task<DecisionSummaryDto?> SetNotesAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            Guid decisionId,
            string? notes,
            CancellationToken cancellationToken)
        {
            var decision = await _dbContext.Decisions
                .FirstOrDefaultAsync(existing =>
                        existing.Id == decisionId
                        && existing.RepositoryConfigId == repositoryConfigId
                        && existing.SolutionId == solution.SolutionId,
                    cancellationToken);
            if (decision is null) return null;

            decision.Notes = notes;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToSummary(decision);
        }

        public async Task<DecisionSummaryDto?> ValidateAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            Guid decisionId,
            CancellationToken cancellationToken)
        {
            var decision = await _dbContext.Decisions
                .FirstOrDefaultAsync(existing =>
                        existing.Id == decisionId
                        && existing.RepositoryConfigId == repositoryConfigId
                        && existing.SolutionId == solution.SolutionId,
                    cancellationToken);
            if (decision is null) return null;

            decision.IsValid = ValidatePayload(decision.DecisionType, decision.PayloadJson, solution);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToSummary(decision);
        }

        private static IQueryable<Decision> ApplySort(IQueryable<Decision> query, string? sort, bool descending)
        {
            return (sort ?? "age").ToLowerInvariant() switch
            {
                "type" => descending
                    ? query.OrderByDescending(decision => decision.DecisionType)
                    : query.OrderBy(decision => decision.DecisionType),
                _ => descending
                    ? query.OrderByDescending(decision => decision.DiscoveredUtc)
                    : query.OrderBy(decision => decision.DiscoveredUtc)
            };
        }

        private async Task DiscoverResolveInterfaceDecisions(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            Dictionary<string, Decision> existing,
            CancellationToken cancellationToken)
        {
            foreach (var method in EnumerateInterfaceMethods(solution))
            {
                var resolution = await _implementationDecisionService.ResolveAsync(
                    workspace,
                    solution,
                    repositoryConfigId,
                    solution.SolutionId,
                    method,
                    true,
                    cancellationToken);

                if (!resolution.DecisionRequired) continue;

                var subjectKey = DocumentationIdUtility.GetDocumentationId(method)
                                 ?? BuildInterfaceMethodSignature(method);
                if (existing.TryGetValue(subjectKey, out var decision)
                    && decision.Status == DecisionStatus.Resolved)
                    continue;

                var interfaceTypeDocId =
                    DocumentationIdUtility.GetDocumentationId(method.ContainingType) ?? string.Empty;
                var interfaceMethodDocId = DocumentationIdUtility.GetDocumentationId(method) ?? string.Empty;
                var payload = new ResolveInterfaceDecisionPayloadDto(
                    GetTypeDisplayName(method.ContainingType),
                    interfaceTypeDocId,
                    interfaceMethodDocId,
                    BuildCandidates(solution, resolution.CandidateTypeNames,
                        resolution.CandidateMethodDocumentationIds),
                    null,
                    null);

                if (decision is null)
                {
                    decision = new Decision
                    {
                        Id = Guid.NewGuid(),
                        RepositoryConfigId = repositoryConfigId,
                        SolutionId = solution.SolutionId,
                        DecisionType = DecisionType.ResolveInterface,
                        Status = DecisionStatus.Pending,
                        IsActive = false,
                        IsValid = true,
                        Name = payload.InterfaceTypeName,
                        SubjectKey = subjectKey,
                        DiscoveredUtc = DateTimeOffset.UtcNow,
                        PayloadJson = JsonSerializer.Serialize(payload, JsonOptions)
                    };
                    _dbContext.Decisions.Add(decision);
                    existing[subjectKey] = decision;
                }
                else
                {
                    decision.Name = payload.InterfaceTypeName;
                    decision.PayloadJson = JsonSerializer.Serialize(payload, JsonOptions);
                    decision.Status = DecisionStatus.Pending;
                }
            }
        }

        private async Task SyncLegacyDecisionsAsync(
            Guid repositoryConfigId,
            string solutionId,
            CancellationToken cancellationToken)
        {
            var legacy = await _dbContext.MethodImplementationDecisions
                .AsNoTracking()
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solutionId)
                .ToListAsync(cancellationToken);
            if (legacy.Count == 0) return;

            var existingKeys = await _dbContext.Decisions
                .Where(decision => decision.RepositoryConfigId == repositoryConfigId
                                   && decision.SolutionId == solutionId
                                   && decision.DecisionType == DecisionType.ResolveInterface)
                .Select(decision => decision.SubjectKey)
                .ToListAsync(cancellationToken);
            var existingKeySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var legacyDecision in legacy)
            {
                var subjectKey = legacyDecision.InterfaceMethodDocumentationId
                                 ?? legacyDecision.InterfaceMethodSignature;
                if (existingKeySet.Contains(subjectKey)) continue;

                var payload = new ResolveInterfaceDecisionPayloadDto(
                    legacyDecision.InterfaceTypeName,
                    legacyDecision.InterfaceTypeDocumentationId ?? string.Empty,
                    legacyDecision.InterfaceMethodDocumentationId ?? string.Empty,
                    Array.Empty<ResolveInterfaceDecisionCandidateDto>(),
                    legacyDecision.ImplementationTypeDocumentationId,
                    legacyDecision.ImplementationMethodDocumentationId);

                _dbContext.Decisions.Add(new Decision
                {
                    Id = legacyDecision.Id,
                    RepositoryConfigId = repositoryConfigId,
                    SolutionId = solutionId,
                    DecisionType = DecisionType.ResolveInterface,
                    Status = DecisionStatus.Resolved,
                    IsActive = true,
                    IsValid = true,
                    Name = legacyDecision.InterfaceTypeName,
                    SubjectKey = subjectKey,
                    DiscoveredUtc = legacyDecision.CreatedUtc,
                    ResolvedUtc = legacyDecision.UpdatedUtc,
                    PayloadJson = JsonSerializer.Serialize(payload, JsonOptions)
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static IEnumerable<IMethodSymbol> EnumerateInterfaceMethods(CodeSolutionWorkspace solution)
        {
            foreach (var compilation in solution.Compilations.Values)
            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            {
                if (type.TypeKind != TypeKind.Interface) continue;

                if (!type.Locations.Any(location => location.IsInSource)) continue;

                foreach (var method in type.GetMembers().OfType<IMethodSymbol>()) yield return method;
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
        {
            foreach (var member in root.GetMembers())
                if (member is INamespaceSymbol ns)
                {
                    foreach (var nested in EnumerateTypes(ns)) yield return nested;
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;
                    foreach (var nestedType in type.GetTypeMembers()) yield return nestedType;
                }
        }

        private static IReadOnlyList<ResolveInterfaceDecisionCandidateDto> BuildCandidates(
            CodeSolutionWorkspace solution,
            IReadOnlyList<string> candidateTypeNames,
            IReadOnlyList<string?> candidateMethodDocIds)
        {
            var candidates = new List<ResolveInterfaceDecisionCandidateDto>();
            var seenTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var methodDocId in candidateMethodDocIds.Where(docId => !string.IsNullOrWhiteSpace(docId)))
            {
                var method = DocumentationIdUtility.FindMethodByDocumentationId(solution, methodDocId!);
                var type = method?.ContainingType;
                var typeName = type is null ? string.Empty : GetTypeDisplayName(type);
                var typeDocId = DocumentationIdUtility.GetDocumentationId(type);
                candidates.Add(new ResolveInterfaceDecisionCandidateDto(typeName, typeDocId, methodDocId));
                if (!string.IsNullOrWhiteSpace(typeName)) seenTypeNames.Add(typeName);
            }

            foreach (var typeName in candidateTypeNames)
            {
                if (seenTypeNames.Contains(typeName)) continue;

                candidates.Add(new ResolveInterfaceDecisionCandidateDto(typeName, null, null));
            }

            return candidates;
        }

        private static string? SerializePayload(DecisionType type, object payload)
        {
            if (type == DecisionType.ResolveInterface && payload is ResolveInterfaceDecisionPayloadDto typed)
                return JsonSerializer.Serialize(typed, JsonOptions);

            return null;
        }

        private DecisionSummaryDto ToSummary(Decision decision)
        {
            object? payload = null;
            if (decision.DecisionType == DecisionType.ResolveInterface)
                payload = JsonSerializer.Deserialize<ResolveInterfaceDecisionPayloadDto>(
                    decision.PayloadJson,
                    JsonOptions);
            else
                payload = decision.PayloadJson;

            return new DecisionSummaryDto(
                decision.Id,
                decision.DecisionType,
                decision.Status,
                decision.IsActive,
                decision.IsValid,
                decision.Name,
                decision.SubjectKey,
                decision.DiscoveredUtc,
                decision.ResolvedUtc,
                decision.Notes,
                payload);
        }

        private static bool ValidatePayload(DecisionType type, string payloadJson, CodeSolutionWorkspace solution)
        {
            if (type != DecisionType.ResolveInterface) return true;

            var payload = JsonSerializer.Deserialize<ResolveInterfaceDecisionPayloadDto>(payloadJson, JsonOptions);
            if (payload is null) return false;

            if (!IsDocIdValid(payload.InterfaceTypeDocId, solution,
                    DocumentationIdUtility.FindTypeByDocumentationId)) return false;

            if (!IsDocIdValid(payload.InterfaceMethodDocId, solution,
                    DocumentationIdUtility.FindMethodByDocumentationId)) return false;

            foreach (var candidate in payload.Candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate.TypeDocId)
                    && !IsDocIdValid(candidate.TypeDocId, solution, DocumentationIdUtility.FindTypeByDocumentationId))
                    return false;

                if (!string.IsNullOrWhiteSpace(candidate.MethodDocId)
                    && !IsDocIdValid(candidate.MethodDocId, solution,
                        DocumentationIdUtility.FindMethodByDocumentationId))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(payload.SelectedTypeDocId)
                && !IsDocIdValid(payload.SelectedTypeDocId, solution, DocumentationIdUtility.FindTypeByDocumentationId))
                return false;

            if (!string.IsNullOrWhiteSpace(payload.SelectedMethodDocId)
                && !IsDocIdValid(payload.SelectedMethodDocId, solution,
                    DocumentationIdUtility.FindMethodByDocumentationId))
                return false;

            return true;
        }

        private static bool IsDocIdValid<TSymbol>(
            string docId,
            CodeSolutionWorkspace solution,
            Func<CodeSolutionWorkspace, string, TSymbol?> finder)
            where TSymbol : class
        {
            return !string.IsNullOrWhiteSpace(docId) && finder(solution, docId) is not null;
        }

        private static string BuildInterfaceMethodSignature(IMethodSymbol method)
        {
            var namespaceName = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var typeName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ??
                           string.Empty;
            var methodName = method.MethodKind == MethodKind.Constructor
                ? method.ContainingType?.Name ?? method.Name
                : method.Name;
            var parameters = method.Parameters
                .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            var parameterList = string.Join(",", parameters);

            if (string.IsNullOrWhiteSpace(parameterList)) parameterList = "none";

            return $"{namespaceName}.{typeName}.{methodName}.{parameterList}";
        }

        private static string GetTypeDisplayName(INamedTypeSymbol symbol)
        {
            var display = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return display.StartsWith("global::", StringComparison.Ordinal)
                ? display.Substring("global::".Length)
                : display;
        }
    }
}