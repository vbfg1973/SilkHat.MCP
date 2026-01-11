using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SilkHat.Code.Analysis.Services.Complexity;
using Xunit;

namespace SilkHat.Tests.Services;

public sealed class ComplexityMathTests
{
    [Theory]
    [InlineData("int Foo(){ return 0; }", 0, 1)]
    [InlineData("int Foo(int x){ if (x > 0) return 1; return 0; }", 1, 2)]
    [InlineData("int Foo(int x, int y){ if (x > 0 && y > 0) return 1; return 0; }", 2, 3)]
    [InlineData("int Foo(int x, int y){ if (x > 0){ if (y > 0) return 1; } return 0; }", 3, 3)]
    [InlineData("int Foo(int x){ switch (x){ case 1: return 1; case 2: return 2; default: return 0; } }", 1, 3)]
    public async Task Computes_Cognitive_And_Cyclomatic_As_Expected(string methodBody, int expectedCognitive, int expectedCyclomatic)
    {
        var (methodSyntax, semanticModel) = await CompileMethodAsync(methodBody);
        var sourceText = methodSyntax.SyntaxTree.GetText();

        var cognitive = new CognitiveComplexityStrategy().Compute(methodSyntax, semanticModel, sourceText);
        var cyclomatic = new CyclomaticComplexityStrategy().Compute(methodSyntax, semanticModel, sourceText);

        Assert.Equal(expectedCognitive, cognitive);
        Assert.Equal(expectedCyclomatic, cyclomatic);
    }

    private static async Task<(BaseMethodDeclarationSyntax MethodSyntax, SemanticModel SemanticModel)> CompileMethodAsync(string methodText)
    {
        await Task.Yield();
        var code = $$"""
        namespace TestHarness
        {
            public class Sample
            {
                {{methodText}}
            }
        }
        """;

        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(code));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestHarness",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        var methodSyntax = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .First();

        // Force semantic model to be ready for the method
        _ = semanticModel.GetDeclaredSymbol(methodSyntax);

        return (methodSyntax, semanticModel);
    }
}
