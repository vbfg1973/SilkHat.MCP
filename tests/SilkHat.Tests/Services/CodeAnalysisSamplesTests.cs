using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services
{
    public sealed class CodeAnalysisSamplesTests
    {
        [Fact]
        public async Task LoadSampleSolution_IndexesProjectsAndTypes()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);

            var solution = Assert.Single(workspace.Solutions.Values);
            Assert.NotEmpty(solution.Projects);
            Assert.Equal(2, solution.Projects.Count);
            Assert.Contains(solution.Namespaces, ns => ns == "SilkHat.Sample.App");
            Assert.Contains(solution.Namespaces, ns => ns == "SilkHat.Sample.Lib");
            Assert.Contains(solution.Compilations, entry => entry.Value is not null);
            Assert.Contains(solution.TreeEntries,
                entry => entry.Type == CodeTreeEntryType.Project && entry.ProjectName == "SilkHat.Sample.App");
            Assert.Contains(solution.TreeEntries,
                entry => entry.Type == CodeTreeEntryType.File &&
                         entry.DisplayPath.Contains("GreetingService.cs", StringComparison.OrdinalIgnoreCase));

            AssertNamedType(solution, "GreetingService", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "IGreetingProvider", NamedTypeKind.Interface, "SilkHat.Sample.App");
            AssertNamedType(solution, "FriendlyGreetingProvider", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "FormalGreetingProvider", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "ComplexitySamples", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "StatusHelper", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "PublicEntry", NamedTypeKind.Class, "SilkHat.Sample.App");
            AssertNamedType(solution, "SampleEnum", NamedTypeKind.Enum, "SilkHat.Sample.App");
            AssertNamedType(solution, "SampleStruct", NamedTypeKind.Struct, "SilkHat.Sample.App");
            AssertNamedType(solution, "SampleRecord", NamedTypeKind.Record, "SilkHat.Sample.App");
            AssertNamedType(solution, "SampleRecordStruct", NamedTypeKind.Struct, "SilkHat.Sample.App");
            AssertNamedType(solution, "SampleDelegate", NamedTypeKind.Delegate, "SilkHat.Sample.App");
            AssertNamedType(solution, "IClock", NamedTypeKind.Interface, "SilkHat.Sample.Lib");
            AssertNamedType(solution, "SystemClock", NamedTypeKind.Class, "SilkHat.Sample.Lib");
            AssertNamedType(solution, "LibConstants", NamedTypeKind.Class, "SilkHat.Sample.Lib");
        }

        [Fact]
        public async Task LoadSampleSolution_IncludesSourceFilePaths()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);

            var solution = Assert.Single(workspace.Solutions.Values);
            var greeting = solution.NamedTypes.First(type => type.Name == "GreetingService");
            Assert.False(string.IsNullOrWhiteSpace(greeting.FilePath));
            Assert.EndsWith("GreetingService.cs", greeting.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(greeting.FilePath!.Contains("SilkHat.Sample.App", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task LoadSampleSolution_PopulatesProjectIndex()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);

            var solution = Assert.Single(workspace.Solutions.Values);
            var appProject = solution.Projects.Values.Single(project => project.Name == "SilkHat.Sample.App");
            var libProject = solution.Projects.Values.Single(project => project.Name == "SilkHat.Sample.Lib");

            Assert.False(string.IsNullOrWhiteSpace(appProject.ProjectKey));
            Assert.Equal("C#", appProject.Language);
            Assert.Equal("SilkHat.Sample.App", appProject.AssemblyName);
            Assert.Single(appProject.References);
            Assert.Equal(libProject.ProjectKey, appProject.References[0].ProjectKey);

            Assert.False(string.IsNullOrWhiteSpace(libProject.ProjectKey));
            Assert.Equal("C#", libProject.Language);
            Assert.Equal("SilkHat.Sample.Lib", libProject.AssemblyName);
            Assert.Empty(libProject.References);
            Assert.Single(libProject.ReferencedBy);
            Assert.Equal(appProject.ProjectKey, libProject.ReferencedBy[0].ProjectKey);
        }

        [Fact]
        public async Task LoadSampleSolution_SymbolKeyLookupMatchesNamedTypes()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);

            var solution = Assert.Single(workspace.Solutions.Values);
            var projectKey = solution.Projects.Values.Single(project => project.Name == "SilkHat.Sample.App")
                .ProjectKey;
            Assert.True(solution.Compilations.TryGetValue(projectKey, out var compilation));

            var symbol = compilation!.GetTypeByMetadataName("SilkHat.Sample.App.GreetingService");
            Assert.NotNull(symbol);

            var key = SymbolKeyUtility.GetSymbolKeyString(symbol!, compilation);
            Assert.True(solution.NamedTypesBySymbolKey.TryGetValue(key, out var dto));
            Assert.Equal("GreetingService", dto!.Name);
            Assert.Equal("SilkHat.Sample.App", dto.Namespace);
        }

        private static void AssertNamedType(CodeSolutionWorkspace solution, string name, NamedTypeKind kind,
            string expectedNamespace)
        {
            var match = solution.NamedTypes.FirstOrDefault(type => type.Name == name && type.Kind == kind);
            Assert.NotNull(match);
            Assert.Equal(expectedNamespace, match!.Namespace);
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