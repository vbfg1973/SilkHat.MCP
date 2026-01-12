using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface ISymbolDescriptionService
    {
        Task<SymbolDescriptionResult<NamedTypeDescriptionDto>> DescribeNamedTypeAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);

        Task<SymbolDescriptionResult<MethodDescriptionDto>> DescribeMethodAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);

        Task<SymbolDescriptionResult<PropertyDescriptionDto>> DescribePropertyAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);

        Task<SymbolDescriptionResult<FieldDescriptionDto>> DescribeFieldAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);

        Task<SymbolDescriptionResult<EventDescriptionDto>> DescribeEventAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);

        Task<SymbolDescriptionResult<NamespaceDescriptionDto>> DescribeNamespaceAsync(
            CodeSolutionWorkspace solution,
            CodeRepositoryWorkspace workspace,
            string documentationId,
            CancellationToken cancellationToken);
    }
}