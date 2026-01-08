using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class MethodCallStackServiceTests
{
    [Fact]
    public async Task BuildCallStack_ReportsInterfaceDecisionRequired()
    {
        var (workspace, solution, symbolKey) = await LoadRunSymbolAsync();
        var decisionService = new Mock<IMethodImplementationDecisionService>();
        decisionService.Setup(service => service.ResolveAsync(
                It.IsAny<CodeRepositoryWorkspace>(),
                It.IsAny<CodeSolutionWorkspace>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IMethodSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MethodImplementationResolution(
                null,
                null,
                true,
                new[]
                {
                    "SilkHat.Sample.App.FriendlyGreetingProvider",
                    "SilkHat.Sample.App.FormalGreetingProvider"
                },
                new[] { "M:SilkHat.Sample.App.FriendlyGreetingProvider.GetGreeting(System.String)", "M:SilkHat.Sample.App.FormalGreetingProvider.GetGreeting(System.String)" }));

        var service = new MethodCallStackService(decisionService.Object);
        var result = await service.BuildCallStackAsync(
            workspace,
            solution,
            Guid.NewGuid(),
            symbolKey,
            null,
            CancellationToken.None);

        var interfaceNode = Assert.Single(result.Nodes, node => node.IsInterfaceTarget);
        Assert.True(interfaceNode.DecisionRequired);
        Assert.Equal("GreetingService", interfaceNode.CallerTypeName);
        Assert.Equal("GetGreeting", interfaceNode.MethodName);
        Assert.Contains("SilkHat.Sample.App.FriendlyGreetingProvider", interfaceNode.CandidateTypeNames);
        Assert.Contains("SilkHat.Sample.App.FormalGreetingProvider", interfaceNode.CandidateTypeNames);
        decisionService.Verify(service => service.ResolveAsync(
            It.IsAny<CodeRepositoryWorkspace>(),
            It.IsAny<CodeSolutionWorkspace>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.Is<IMethodSymbol>(symbol => symbol.Name == "GetGreeting"),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BuildCallStack_UsesResolvedImplementation_WhenDecisionProvided()
    {
        var (workspace, solution, symbolKey) = await LoadRunSymbolAsync();
        var implementation = FindMethodSymbol(solution, "SilkHat.Sample.App.FriendlyGreetingProvider", "GetGreeting");
        var decisionId = Guid.NewGuid();

        var decisionService = new Mock<IMethodImplementationDecisionService>();
        decisionService.Setup(service => service.ResolveAsync(
                It.IsAny<CodeRepositoryWorkspace>(),
                It.IsAny<CodeSolutionWorkspace>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IMethodSymbol>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MethodImplementationResolution(
                implementation,
                new DecisionUsage(decisionId, "InterfaceImplementation"),
                false,
                Array.Empty<string>(),
                Array.Empty<string?>()));

        var service = new MethodCallStackService(decisionService.Object);
        var result = await service.BuildCallStackAsync(
            workspace,
            solution,
            Guid.NewGuid(),
            symbolKey,
            null,
            CancellationToken.None);

        var interfaceNode = Assert.Single(result.Nodes, node => node.IsInterfaceTarget);
        Assert.False(interfaceNode.DecisionRequired);
        Assert.NotNull(interfaceNode.Decision);
        Assert.Equal(decisionId, interfaceNode.Decision!.Id);
        Assert.Equal("FriendlyGreetingProvider", interfaceNode.TypeName);
        Assert.Equal("SilkHat.Sample.App.FriendlyGreetingProvider", interfaceNode.ResolvedTypeName);
    }

    private static async Task<(CodeRepositoryWorkspace Workspace, CodeSolutionWorkspace Solution, string SymbolKey)>
        LoadRunSymbolAsync()
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
            && item.DisplayPath.EndsWith("GreetingService.cs", StringComparison.OrdinalIgnoreCase));

        var symbolsService = new CodeSymbolOutlineService();
        var symbolsResult = await symbolsService.GetFileSymbolsAsync(
            workspace,
            solution,
            entry.RepositoryPath,
            CancellationToken.None);

        var publicEntry = symbolsResult.Symbols!.First(node => node.Name == "PublicEntry");
        var runSymbol = publicEntry.Children.First(child => child.Name == "Run");

        return (workspace, solution, runSymbol.SymbolKey);
    }

    private static IMethodSymbol FindMethodSymbol(
        CodeSolutionWorkspace solution,
        string typeMetadataName,
        string methodName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null)
            {
                continue;
            }

            return type.GetMembers()
                .OfType<IMethodSymbol>()
                .First(member => member.Name == methodName);
        }

        throw new InvalidOperationException($"Type {typeMetadataName} not found.");
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
