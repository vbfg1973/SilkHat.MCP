using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Api.Services;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Services;

public sealed class DecisionServiceTests
{
    [Fact]
    public async Task DiscoverPendingAsync_CreatesResolveInterfaceDecision()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var repositoryId = Guid.NewGuid();

        var implementationService = new MethodImplementationDecisionService(dbContext);
        var service = new DecisionService(
            dbContext,
            implementationService,
            NullLogger<DecisionService>.Instance);

        var results = await service.DiscoverPendingAsync(
            workspace,
            solution,
            repositoryId,
            DecisionType.ResolveInterface,
            CancellationToken.None);

        Assert.Contains(results, decision =>
            decision.Type == DecisionType.ResolveInterface
            && decision.Status == DecisionStatus.Pending
            && decision.Name.Contains("IGreetingProvider", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolveAsync_UpdatesDecisionPayload()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var repositoryId = Guid.NewGuid();
        var decisionId = Guid.NewGuid();

        var interfaceMethod = FindInterfaceMethod(solution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting");
        var implementationMethod = FindMethodSymbol(solution, "SilkHat.Sample.App.FriendlyGreetingProvider", "GetGreeting");
        var interfaceDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethod.ContainingType)!;
        var interfaceMethodDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethod)!;
        var implementationTypeDocId = DocumentationIdUtility.GetDocumentationId(implementationMethod.ContainingType)!;
        var implementationMethodDocId = DocumentationIdUtility.GetDocumentationId(implementationMethod)!;

        var payload = new ResolveInterfaceDecisionPayloadDto(
            "SilkHat.Sample.App.IGreetingProvider",
            interfaceDocId,
            interfaceMethodDocId,
            new[]
            {
                new ResolveInterfaceDecisionCandidateDto(
                    "SilkHat.Sample.App.FriendlyGreetingProvider",
                    implementationTypeDocId,
                    implementationMethodDocId)
            },
            null,
            null);

        dbContext.Decisions.Add(new Decision
        {
            Id = decisionId,
            RepositoryConfigId = repositoryId,
            SolutionId = solution.SolutionId,
            DecisionType = DecisionType.ResolveInterface,
            Status = DecisionStatus.Pending,
            IsActive = false,
            IsValid = true,
            Name = payload.InterfaceTypeName,
            SubjectKey = interfaceMethodDocId,
            DiscoveredUtc = DateTimeOffset.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
        });
        await dbContext.SaveChangesAsync();

        var implementationService = new MethodImplementationDecisionService(dbContext);
        var service = new DecisionService(
            dbContext,
            implementationService,
            NullLogger<DecisionService>.Instance);

        var resolvedPayload = payload with
        {
            SelectedTypeDocId = implementationTypeDocId,
            SelectedMethodDocId = implementationMethodDocId
        };

        var result = await service.ResolveAsync(
            workspace,
            solution,
            repositoryId,
            decisionId,
            DecisionType.ResolveInterface,
            resolvedPayload,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DecisionStatus.Resolved, result!.Status);
        Assert.True(result.IsActive);
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

    private static IMethodSymbol FindInterfaceMethod(
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
