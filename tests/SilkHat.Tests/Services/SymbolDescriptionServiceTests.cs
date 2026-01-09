using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class SymbolDescriptionServiceTests
{
    [Fact]
    public async Task DescribeNamedType_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetTypeDocId(solution, "SilkHat.Sample.Lib.SystemClock");
        var service = new SymbolDescriptionService();

        var result = await service.DescribeNamedTypeAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.Equal("SystemClock", result.Description!.Name);
        Assert.Equal("Class", result.Description!.TypeKind);
        Assert.NotNull(result.Description!.Location);
    }

    [Fact]
    public async Task DescribeMethod_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetMethodDocId(solution, "SilkHat.Sample.App.StatusHelper", "GetHTTPStatus");
        var service = new SymbolDescriptionService();

        var result = await service.DescribeMethodAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.Equal("GetHTTPStatus", result.Description!.Name);
        Assert.Single(result.Description!.Parameters);
        Assert.NotNull(result.Description!.Location);
    }

    [Fact]
    public async Task DescribeProperty_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetPropertyDocId(solution, "SilkHat.Sample.App.StatusHelper", "Grade11PlusScore");
        var service = new SymbolDescriptionService();

        var result = await service.DescribePropertyAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.True(result.Description!.HasGetter);
        Assert.True(result.Description!.HasSetter);
    }

    [Fact]
    public async Task DescribeField_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetFieldDocId(solution, "SilkHat.Sample.Lib.LibConstants", "DefaultLabel");
        var service = new SymbolDescriptionService();

        var result = await service.DescribeFieldAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.True(result.Description!.Modifiers.IsConst);
    }

    [Fact]
    public async Task DescribeEvent_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetEventDocId(solution, "SilkHat.Sample.App.StatusHelper", "StatusChecked");
        var service = new SymbolDescriptionService();

        var result = await service.DescribeEventAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.Equal("StatusChecked", result.Description!.Name);
    }

    [Fact]
    public async Task DescribeNamespace_ReturnsDetails()
    {
        var (workspace, solution) = await LoadWorkspaceAsync();
        var docId = GetNamespaceDocId(solution, "SilkHat.Sample.App");
        var service = new SymbolDescriptionService();

        var result = await service.DescribeNamespaceAsync(solution, workspace, docId, CancellationToken.None);

        Assert.Equal(SymbolDescriptionStatus.Success, result.Status);
        Assert.NotNull(result.Description);
        Assert.Equal("SilkHat.Sample.App", result.Description!.FullName);
    }

    private static string GetMethodDocId(CodeSolutionWorkspace solution, string typeMetadataName, string methodName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            var method = type.GetMembers()
                .OfType<IMethodSymbol>()
                .First(member => member.Name == methodName);
            var docId = DocumentationIdUtility.GetDocumentationId(method);
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Method {typeMetadataName}.{methodName} not found.");
    }

    private static string GetTypeDocId(CodeSolutionWorkspace solution, string typeMetadataName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            var docId = type.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Type {typeMetadataName} not found.");
    }

    private static string GetPropertyDocId(CodeSolutionWorkspace solution, string typeMetadataName, string propertyName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            var property = type.GetMembers()
                .OfType<IPropertySymbol>()
                .First(member => member.Name == propertyName);
            var docId = DocumentationIdUtility.GetDocumentationId(property);
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Property {typeMetadataName}.{propertyName} not found.");
    }

    private static string GetFieldDocId(CodeSolutionWorkspace solution, string typeMetadataName, string fieldName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            var field = type.GetMembers()
                .OfType<IFieldSymbol>()
                .First(member => member.Name == fieldName);
            var docId = DocumentationIdUtility.GetDocumentationId(field);
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Field {typeMetadataName}.{fieldName} not found.");
    }

    private static string GetEventDocId(CodeSolutionWorkspace solution, string typeMetadataName, string eventName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            var @event = type.GetMembers()
                .OfType<IEventSymbol>()
                .First(member => member.Name == eventName);
            var docId = DocumentationIdUtility.GetDocumentationId(@event);
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Event {typeMetadataName}.{eventName} not found.");
    }

    private static string GetNamespaceDocId(CodeSolutionWorkspace solution, string namespaceName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var @namespace = FindNamespace(compilation.GlobalNamespace, namespaceName);
            if (@namespace is null)
            {
                continue;
            }

            var docId = DocumentationIdUtility.GetDocumentationId(@namespace);
            if (!string.IsNullOrWhiteSpace(docId))
            {
                return docId;
            }
        }

        throw new InvalidOperationException($"Namespace {namespaceName} not found.");
    }

    private static INamespaceSymbol? FindNamespace(INamespaceSymbol root, string fullName)
    {
        if (string.Equals(root.ToDisplayString(), fullName, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var member in root.GetMembers().OfType<INamespaceSymbol>())
        {
            var match = FindNamespace(member, fullName);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static async Task<(CodeRepositoryWorkspace Workspace, CodeSolutionWorkspace Solution)> LoadWorkspaceAsync()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
            CancellationToken.None);
        var solution = Assert.Single(workspace.Solutions.Values);
        return (workspace, solution);
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
