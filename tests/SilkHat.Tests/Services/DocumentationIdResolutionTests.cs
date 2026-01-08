using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class DocumentationIdResolutionTests
{
    [Fact]
    public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForMethods()
    {
        var (firstWorkspace, firstSolution) = await LoadWorkspaceAsync();
        var docIds = new[]
        {
            GetMethodDocId(firstSolution, "SilkHat.Sample.Lib.IClock", "get_Now"),
            GetMethodDocId(firstSolution, "SilkHat.Sample.Lib.SystemClock", "get_Now"),
            GetMethodDocId(firstSolution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting"),
            GetMethodDocId(firstSolution, "SilkHat.Sample.App.FriendlyGreetingProvider", "GetGreeting"),
            GetMethodDocId(firstSolution, "SilkHat.Sample.App.PublicEntry", "Run")
        };

        var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
        foreach (var docId in docIds)
        {
            var resolved = DocumentationIdUtility.FindMethodByDocumentationId(secondSolution, docId);
            Assert.NotNull(resolved);
            Assert.Equal(docId, resolved!.GetDocumentationCommentId());
        }
    }

    [Fact]
    public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForTypes()
    {
        var (firstWorkspace, firstSolution) = await LoadWorkspaceAsync();
        var docIds = new[]
        {
            GetTypeDocId(firstSolution, "SilkHat.Sample.Lib.IClock"),
            GetTypeDocId(firstSolution, "SilkHat.Sample.Lib.SystemClock"),
            GetTypeDocId(firstSolution, "SilkHat.Sample.App.FriendlyGreetingProvider"),
            GetTypeDocId(firstSolution, "SilkHat.Sample.App.PublicEntry")
        };

        var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
        foreach (var docId in docIds)
        {
            var resolved = DocumentationIdUtility.FindTypeByDocumentationId(secondSolution, docId);
            Assert.NotNull(resolved);
            Assert.Equal(docId, resolved!.GetDocumentationCommentId());
        }
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
