using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services.Complexity
{
    public sealed class IndentationComplexityStrategy : IComplexityStrategy
    {
        private const int TabSize = 4;
        public ComplexityMeasureType MeasureType => ComplexityMeasureType.Indentation;

        public int Compute(BaseMethodDeclarationSyntax method, SemanticModel semanticModel, SourceText sourceText)
        {
            var span = method.GetLocation().GetLineSpan();
            var startLine = span.StartLinePosition.Line;
            var endLine = span.EndLinePosition.Line;

            if (startLine < 0 || endLine < startLine || endLine >= sourceText.Lines.Count) return 0;

            var baseIndent = CountIndentation(sourceText.Lines[startLine].ToString());
            var total = 0;

            for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
            {
                var lineText = sourceText.Lines[lineIndex].ToString();
                if (string.IsNullOrWhiteSpace(lineText)) continue;

                var indent = CountIndentation(lineText);
                var normalized = indent - baseIndent;
                if (normalized < 0) normalized = 0;

                total += normalized;
            }

            return total;
        }

        private static int CountIndentation(string line)
        {
            var count = 0;
            foreach (var ch in line)
            {
                if (ch == ' ')
                {
                    count++;
                    continue;
                }

                if (ch == '\t')
                {
                    count += TabSize;
                    continue;
                }

                break;
            }

            return count;
        }
    }
}