using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Analysis.Services.Complexity;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services
{
    public sealed class MethodComplexityServiceTests
    {
        [Fact]
        public async Task GetMethodComplexity_ReturnsExpectedResult()
        {
            var (solution, docId) = await LoadSolutionAndMethodDocIdAsync();
            var factory = BuildFactory();
            var service = new MethodComplexityService(factory);

            var result = await service.GetMethodComplexityAsync(
                solution,
                docId,
                ComplexityMeasureType.Cyclomatic,
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(docId, result!.DocumentationId);
            Assert.Equal(ComplexityMeasureType.Cyclomatic, result.MeasureType);
            Assert.Equal(ComplexityTargetKind.Method, result.TargetKind);
            Assert.Equal(9, result.Value);
        }

        private static IComplexityStrategyFactory BuildFactory()
        {
            var strategies = new IComplexityStrategy[]
            {
                new CognitiveComplexityStrategy(),
                new CyclomaticComplexityStrategy(),
                new IndentationComplexityStrategy()
            };
            return new ComplexityStrategyFactory(strategies);
        }

        private static async Task<(CodeSolutionWorkspace Solution, string DocId)> LoadSolutionAndMethodDocIdAsync()
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
                var docId = DocumentationIdUtility.GetDocumentationId(method);
                if (!string.IsNullOrWhiteSpace(docId)) return (solution, docId);
            }

            throw new InvalidOperationException("CalculateScore docId not found.");
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