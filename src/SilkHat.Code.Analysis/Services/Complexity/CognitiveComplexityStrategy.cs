using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services.Complexity
{
    public sealed class CognitiveComplexityStrategy : IComplexityStrategy
    {
        public ComplexityMeasureType MeasureType => ComplexityMeasureType.Cognitive;

        public int Compute(BaseMethodDeclarationSyntax method, SemanticModel semanticModel, SourceText sourceText)
        {
            var walker = new CognitiveWalker();
            walker.Visit(method);
            return walker.Complexity;
        }

        private sealed class CognitiveWalker : CSharpSyntaxWalker
        {
            private int _depth;
            public int Complexity { get; private set; }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                AddDecision(node.Condition);
                VisitWithDepth(node.Statement);
                if (node.Else is not null) Visit(node.Else);
            }

            public override void VisitElseClause(ElseClauseSyntax node)
            {
                Visit(node.Statement);
            }

            public override void VisitForStatement(ForStatementSyntax node)
            {
                AddDecision(node.Condition);
                VisitWithDepth(node.Statement);
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                AddDecision(null);
                VisitWithDepth(node.Statement);
            }

            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                AddDecision(node.Condition);
                VisitWithDepth(node.Statement);
            }

            public override void VisitDoStatement(DoStatementSyntax node)
            {
                AddDecision(node.Condition);
                VisitWithDepth(node.Statement);
            }

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                AddDecision(node.Expression);
                _depth++;
                foreach (var section in node.Sections) Visit(section);
                _depth--;
            }

            public override void VisitCatchClause(CatchClauseSyntax node)
            {
                AddDecision(node.Filter?.FilterExpression);
                VisitWithDepth(node.Block);
            }

            public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                AddDecision(node.Condition);
                _depth++;
                Visit(node.WhenTrue);
                Visit(node.WhenFalse);
                _depth--;
            }

            private void AddDecision(ExpressionSyntax? condition)
            {
                Complexity += 1 + _depth;
                Complexity += CountLogicalOperators(condition);
            }

            private void VisitWithDepth(CSharpSyntaxNode? node)
            {
                if (node is null) return;

                _depth++;
                Visit(node);
                _depth--;
            }

            private static int CountLogicalOperators(ExpressionSyntax? expression)
            {
                if (expression is null) return 0;

                var counter = new LogicalOperatorCounter();
                counter.Visit(expression);
                return counter.Count;
            }
        }

        private sealed class LogicalOperatorCounter : CSharpSyntaxWalker
        {
            public int Count { get; private set; }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.LogicalAndExpression) ||
                    node.IsKind(SyntaxKind.LogicalOrExpression)) Count++;

                base.VisitBinaryExpression(node);
            }
        }
    }
}