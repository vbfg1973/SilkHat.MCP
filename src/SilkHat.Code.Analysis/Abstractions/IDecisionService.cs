using SilkHat.Code.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IDecisionService
{
    Task<IReadOnlyList<DecisionSummaryDto>> DiscoverPendingAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        DecisionType? type,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DecisionSummaryDto>> GetPendingAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        DecisionType? type,
        string? sort,
        bool descending,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DecisionSummaryDto>> GetResolvedAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        DecisionType? type,
        bool? active,
        string? sort,
        bool descending,
        CancellationToken cancellationToken);

    Task<DecisionSummaryDto?> ResolveAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        Guid decisionId,
        DecisionType type,
        object payload,
        CancellationToken cancellationToken);

    Task<DecisionSummaryDto?> SetActiveAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        Guid decisionId,
        bool isActive,
        CancellationToken cancellationToken);

    Task<DecisionSummaryDto?> SetNotesAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        Guid decisionId,
        string? notes,
        CancellationToken cancellationToken);

    Task<DecisionSummaryDto?> ValidateAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        Guid decisionId,
        CancellationToken cancellationToken);
}
