using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface IComplexityStrategy
    {
        ComplexityMeasureType MeasureType { get; }

        int Compute(BaseMethodDeclarationSyntax method, SemanticModel semanticModel, SourceText sourceText);
    }
}