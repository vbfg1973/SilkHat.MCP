using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class CodeAnalysisSamplesTests
{
    [Fact]
    public async Task LoadSampleSolution_IndexesProjectsAndTypes()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { "./SilkHat.Sample.sln" },
            CancellationToken.None);

        Assert.NotEmpty(workspace.Projects);
        Assert.Equal(2, workspace.Projects.Count);
        Assert.Contains(workspace.Namespaces, ns => ns == "SilkHat.Sample.App");
        Assert.Contains(workspace.Namespaces, ns => ns == "SilkHat.Sample.Lib");
        Assert.Contains(workspace.Compilations, entry => entry.Value is not null);
        Assert.Contains(workspace.TreeEntries, entry => entry.Type == CodeTreeEntryType.Project && entry.ProjectName == "SilkHat.Sample.App");
        Assert.Contains(workspace.TreeEntries, entry => entry.Type == CodeTreeEntryType.File && entry.DisplayPath.Contains("GreetingService.cs", StringComparison.OrdinalIgnoreCase));

        AssertNamedType(workspace, "GreetingService", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "IGreetingProvider", NamedTypeKind.Interface, "SilkHat.Sample.App");
        AssertNamedType(workspace, "FriendlyGreetingProvider", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "FormalGreetingProvider", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "ComplexitySamples", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "StatusHelper", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "PublicEntry", NamedTypeKind.Class, "SilkHat.Sample.App");
        AssertNamedType(workspace, "SampleEnum", NamedTypeKind.Enum, "SilkHat.Sample.App");
        AssertNamedType(workspace, "SampleStruct", NamedTypeKind.Struct, "SilkHat.Sample.App");
        AssertNamedType(workspace, "SampleRecord", NamedTypeKind.Record, "SilkHat.Sample.App");
        AssertNamedType(workspace, "SampleRecordStruct", NamedTypeKind.Record, "SilkHat.Sample.App");
        AssertNamedType(workspace, "SampleDelegate", NamedTypeKind.Delegate, "SilkHat.Sample.App");
        AssertNamedType(workspace, "IClock", NamedTypeKind.Interface, "SilkHat.Sample.Lib");
        AssertNamedType(workspace, "SystemClock", NamedTypeKind.Class, "SilkHat.Sample.Lib");
        AssertNamedType(workspace, "LibConstants", NamedTypeKind.Class, "SilkHat.Sample.Lib");
    }

    [Fact]
    public async Task LoadSampleSolution_IncludesSourceFilePaths()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);

        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { "./SilkHat.Sample.sln" },
            CancellationToken.None);

        var greeting = workspace.NamedTypes.First(type => type.Name == "GreetingService");
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
            new[] { "./SilkHat.Sample.sln" },
            CancellationToken.None);

        var appProject = workspace.Projects.Values.Single(project => project.Name == "SilkHat.Sample.App");
        var libProject = workspace.Projects.Values.Single(project => project.Name == "SilkHat.Sample.Lib");

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
            new[] { "./SilkHat.Sample.sln" },
            CancellationToken.None);

        var projectKey = workspace.Projects.Values.Single(project => project.Name == "SilkHat.Sample.App").ProjectKey;
        Assert.True(workspace.Compilations.TryGetValue(projectKey, out var compilation));

        var symbol = compilation!.GetTypeByMetadataName("SilkHat.Sample.App.GreetingService");
        Assert.NotNull(symbol);

        var key = SymbolKeyUtility.GetSymbolKeyString(symbol!, compilation);
        Assert.True(workspace.NamedTypesBySymbolKey.TryGetValue(key, out var dto));
        Assert.Equal("GreetingService", dto!.Name);
        Assert.Equal("SilkHat.Sample.App", dto.Namespace);
    }

    private static void AssertNamedType(CodeRepositoryWorkspace workspace, string name, NamedTypeKind kind, string expectedNamespace)
    {
        var match = workspace.NamedTypes.FirstOrDefault(type => type.Name == name && type.Kind == kind);
        Assert.NotNull(match);
        Assert.Equal(expectedNamespace, match!.Namespace);
    }

    private static string LocateSampleRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", "solution01", "SilkHat.Sample.sln");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate samples/solution01/SilkHat.Sample.sln.");
    }
}
