using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Analysis.Services.Complexity;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services
{
    public sealed class ComplexityStrategyTests
    {
        [Fact]
        public async Task CyclomaticComplexityStrategy_ReturnsExpectedValue()
        {
            var (methodSyntax, semanticModel) = await LoadMethodAsync();
            var strategy = new CyclomaticComplexityStrategy();

            var value = strategy.Compute(methodSyntax, semanticModel, methodSyntax.SyntaxTree.GetText());

            Assert.Equal(9, value);
        }

        [Fact]
        public async Task CognitiveComplexityStrategy_ReturnsExpectedValue()
        {
            var (methodSyntax, semanticModel) = await LoadMethodAsync();
            var strategy = new CognitiveComplexityStrategy();

            var value = strategy.Compute(methodSyntax, semanticModel, methodSyntax.SyntaxTree.GetText());

            Assert.Equal(9, value);
        }

        [Fact]
        public async Task IndentationComplexityStrategy_ReturnsExpectedValue()
        {
            var (methodSyntax, semanticModel) = await LoadMethodAsync();
            var strategy = new IndentationComplexityStrategy();

            var value = strategy.Compute(methodSyntax, semanticModel, methodSyntax.SyntaxTree.GetText());

            Assert.Equal(240, value);
        }

        [Fact]
        public void ComplexityStrategyFactory_ReturnsMatchingStrategy()
        {
            var strategies = new IComplexityStrategy[]
            {
                new CognitiveComplexityStrategy(),
                new CyclomaticComplexityStrategy(),
                new IndentationComplexityStrategy()
            };
            var factory = new ComplexityStrategyFactory(strategies);

            Assert.IsType<CognitiveComplexityStrategy>(factory.GetStrategy(ComplexityMeasureType.Cognitive));
            Assert.IsType<CyclomaticComplexityStrategy>(factory.GetStrategy(ComplexityMeasureType.Cyclomatic));
            Assert.IsType<IndentationComplexityStrategy>(factory.GetStrategy(ComplexityMeasureType.Indentation));
        }

        private static async Task<(BaseMethodDeclarationSyntax MethodSyntax, SemanticModel SemanticModel)>
            LoadMethodAsync()
        {
            var (solution, method) = await LoadSolutionAndMethodAsync();
            var syntaxRef = method.DeclaringSyntaxReferences.First();
            var syntaxNode = syntaxRef.GetSyntax();
            var methodSyntax = Assert.IsAssignableFrom<BaseMethodDeclarationSyntax>(syntaxNode);
            var compilation = solution.Compilations.Values.First(compilation =>
                string.Equals(compilation.AssemblyName, method.ContainingAssembly?.Name,
                    StringComparison.OrdinalIgnoreCase));
            var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree, true);

            return (methodSyntax, semanticModel);
        }

        private static async Task<(CodeSolutionWorkspace Solution, IMethodSymbol Method)> LoadSolutionAndMethodAsync()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);

            var solution = Assert.Single(workspace.Solutions.Values);
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName("SilkHat.Sample.App.ComplexitySamples");
                if (type is null) continue;

                var method = type.GetMembers()
                    .OfType<IMethodSymbol>()
                    .First(member => member.Name == "CalculateScore");
                return (solution, method);
            }

            throw new InvalidOperationException("CalculateScore method not found.");
        }

        private static string LocateSampleRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "samples", "solution01", "SilkHat.Sample.sln");
                if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate samples/solution01/SilkHat.Sample.sln.");
        }
    }
}