using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services
{
    public sealed class CodeSymbolOutlineServiceTests
    {
        [Fact]
        public async Task GetFileSymbols_ReturnsNamedTypesAndMembers()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);
            var solution = Assert.Single(workspace.Solutions.Values);

            var entry = solution.TreeEntries.First(item =>
                item.Type == CodeTreeEntryType.File
                && item.DisplayPath.EndsWith("AnalysisSamples.cs", StringComparison.OrdinalIgnoreCase));

            var service = new CodeSymbolOutlineService();
            var result =
                await service.GetFileSymbolsAsync(workspace, solution, entry.RepositoryPath, CancellationToken.None);

            Assert.Equal(CodeFileSymbolsStatus.Success, result.Status);
            Assert.NotNull(result.Symbols);

            var complexity = result.Symbols!.FirstOrDefault(node => node.Name == "ComplexitySamples");
            Assert.NotNull(complexity);
            Assert.Contains(complexity!.Children,
                child => child.Name == "CalculateScore" && child.RealType == "Method");

            var helper = result.Symbols!.FirstOrDefault(node => node.Name == "StatusHelper");
            Assert.NotNull(helper);
            Assert.Contains(helper!.Children,
                child => child.Name == "Grade11PlusScore" && child.RealType == "Property");
        }

        [Fact]
        public async Task GetFileSymbols_ReturnsInvalidPath_WhenMissing()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);
            var solution = Assert.Single(workspace.Solutions.Values);

            var service = new CodeSymbolOutlineService();
            var result = await service.GetFileSymbolsAsync(workspace, solution, string.Empty, CancellationToken.None);

            Assert.Equal(CodeFileSymbolsStatus.InvalidPath, result.Status);
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