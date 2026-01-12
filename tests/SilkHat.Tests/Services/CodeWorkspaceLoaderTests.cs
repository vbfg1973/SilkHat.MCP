using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services
{
    public sealed class CodeWorkspaceLoaderTests
    {
        [Fact]
        public async Task LoadAsync_Throws_WhenNoSolutionFiles()
        {
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
            var tempRoot = Path.Combine(Path.GetTempPath(), $"silkhat-empty-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);

            try
            {
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    loader.LoadAsync(tempRoot, Array.Empty<SolutionReference>(), CancellationToken.None));
                Assert.Contains("No solution files selected", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public async Task LoadAsync_LoadsMinimalSolution()
        {
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
            var tempRoot = Path.Combine(Path.GetTempPath(), $"silkhat-sln-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);

            var projectId = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var slnPath = Path.Combine(tempRoot, "Sample.sln");
            var csprojPath = Path.Combine(tempRoot, "Sample.csproj");
            var codePath = Path.Combine(tempRoot, "Sample.cs");

            File.WriteAllText(csprojPath, """
                                          <Project Sdk="Microsoft.NET.Sdk">
                                            <PropertyGroup>
                                              <TargetFramework>net9.0</TargetFramework>
                                            </PropertyGroup>
                                          </Project>
                                          """);

            File.WriteAllText(codePath, """
                                        namespace Sample;

                                        public class Foo
                                        {
                                        }
                                        """);

            File.WriteAllText(slnPath, $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""Sample"", ""Sample.csproj"", ""{projectId}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {projectId}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {projectId}.Debug|Any CPU.Build.0 = Debug|Any CPU
    EndGlobalSection
EndGlobal
");

            try
            {
                var workspace = await loader.LoadAsync(
                    tempRoot,
                    new[] { new SolutionReference("./Sample.sln", "solution-1") },
                    CancellationToken.None);

                var solution = Assert.Single(workspace.Solutions.Values);
                Assert.Single(solution.Projects);
                Assert.Contains(solution.Namespaces, ns => ns == "Sample");
                Assert.Contains(solution.NamedTypes, type => type.Name == "Foo");
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}