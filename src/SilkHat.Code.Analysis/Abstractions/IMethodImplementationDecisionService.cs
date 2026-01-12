using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface IMethodImplementationDecisionService
    {
        Task<MethodImplementationResolution> ResolveAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid repositoryConfigId,
            string solutionId,
            IMethodSymbol interfaceMethod,
            bool ignoreStoredDecisions,
            CancellationToken cancellationToken);
    }

    public sealed record MethodImplementationResolution(
        IMethodSymbol? Implementation,
        DecisionUsage? Decision,
        bool DecisionRequired,
        IReadOnlyList<string> CandidateTypeNames,
        IReadOnlyList<string?> CandidateMethodDocumentationIds);
}