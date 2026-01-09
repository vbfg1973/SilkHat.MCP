using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services.Complexity;

public sealed class CyclomaticComplexityStrategy : IComplexityStrategy
{
    public ComplexityMeasureType MeasureType => ComplexityMeasureType.Cyclomatic;

    public int Compute(BaseMethodDeclarationSyntax method, SemanticModel semanticModel, SourceText sourceText)
    {
        var walker = new CyclomaticWalker();
        walker.Visit(method);
        return 1 + walker.DecisionCount;
    }

    private sealed class CyclomaticWalker : CSharpSyntaxWalker
    {
        public int DecisionCount { get; private set; }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Condition);
            base.VisitIfStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Condition);
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            DecisionCount++;
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Condition);
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Condition);
            base.VisitDoStatement(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Filter?.FilterExpression);
            base.VisitCatchClause(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            DecisionCount++;
            DecisionCount += CountLogicalOperators(node.Condition);
            base.VisitConditionalExpression(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            foreach (var section in node.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label is CaseSwitchLabelSyntax)
                    {
                        DecisionCount++;
                    }
                }
            }

            base.VisitSwitchStatement(node);
        }

        private static int CountLogicalOperators(ExpressionSyntax? expression)
        {
            if (expression is null)
            {
                return 0;
            }

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
            if (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression))
            {
                Count++;
            }

            base.VisitBinaryExpression(node);
        }
    }
}
