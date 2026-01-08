using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Api.Services;
using SilkHat.Api.Tests.TestHelpers;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Tests.Services;

public sealed class MethodImplementationDecisionServiceTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsSingleImplementation_WhenOnlyOneExists()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var interfaceType = FindNamedType(solution, "SilkHat.Sample.Lib.IClock");
        var interfaceMethod = interfaceType.GetMembers()
            .OfType<IPropertySymbol>()
            .First(member => member.Name == "Now")
            .GetMethod!;
        var service = new MethodImplementationDecisionService(dbContext);

        var result = await service.ResolveAsync(
            workspace,
            solution,
            Guid.NewGuid(),
            solution.SolutionId,
            interfaceMethod,
            CancellationToken.None);

        Assert.NotNull(result.Implementation);
        Assert.False(result.DecisionRequired);
        Assert.Null(result.Decision);
        Assert.Contains("SilkHat.Sample.Lib.SystemClock", result.CandidateTypeNames);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsDecision_WhenStoredDecisionExists()
    {
        var decisionId = Guid.NewGuid();
        var repositoryConfigId = Guid.NewGuid();
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var interfaceMethod = FindInterfaceMethod(solution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting");
        var signature = BuildInterfaceMethodSignature(interfaceMethod);
        dbContext.MethodImplementationDecisions.Add(new MethodImplementationDecision
        {
            Id = decisionId,
            RepositoryConfigId = repositoryConfigId,
            SolutionId = solution.SolutionId,
            InterfaceTypeName = "SilkHat.Sample.App.IGreetingProvider",
            InterfaceMethodSignature = signature,
            ImplementationTypeName = "SilkHat.Sample.App.FriendlyGreetingProvider"
        });
        await dbContext.SaveChangesAsync();

        var service = new MethodImplementationDecisionService(dbContext);
        var result = await service.ResolveAsync(
            workspace,
            solution,
            repositoryConfigId,
            solution.SolutionId,
            interfaceMethod,
            CancellationToken.None);

        Assert.NotNull(result.Implementation);
        Assert.NotNull(result.Decision);
        Assert.Equal(decisionId, result.Decision!.Id);
        Assert.False(result.DecisionRequired);
        Assert.Equal("FriendlyGreetingProvider", result.Implementation!.ContainingType.Name);
    }

    [Fact]
    public async Task ResolveAsync_UsesImplementationDocId_WhenStoredDecisionProvidesIt()
    {
        var decisionId = Guid.NewGuid();
        var repositoryConfigId = Guid.NewGuid();
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var interfaceMethod = FindInterfaceMethod(solution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting");
        var implementationMethod = FindMethodSymbol(solution, "SilkHat.Sample.App.FriendlyGreetingProvider", "GetGreeting");
        var signature = BuildInterfaceMethodSignature(interfaceMethod);
        var interfaceDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethod);
        var implementationDocId = DocumentationIdUtility.GetDocumentationId(implementationMethod);
        dbContext.MethodImplementationDecisions.Add(new MethodImplementationDecision
        {
            Id = decisionId,
            RepositoryConfigId = repositoryConfigId,
            SolutionId = solution.SolutionId,
            InterfaceTypeName = "SilkHat.Sample.App.IGreetingProvider",
            InterfaceMethodSignature = signature,
            InterfaceMethodDocumentationId = interfaceDocId,
            ImplementationTypeName = "SilkHat.Sample.App.FriendlyGreetingProvider",
            ImplementationMethodDocumentationId = implementationDocId
        });
        await dbContext.SaveChangesAsync();

        var service = new MethodImplementationDecisionService(dbContext);
        var result = await service.ResolveAsync(
            workspace,
            solution,
            repositoryConfigId,
            solution.SolutionId,
            interfaceMethod,
            CancellationToken.None);

        Assert.NotNull(result.Implementation);
        Assert.NotNull(result.Decision);
        Assert.Equal(decisionId, result.Decision!.Id);
        Assert.Equal("FriendlyGreetingProvider", result.Implementation!.ContainingType.Name);
        Assert.Equal(implementationDocId, DocumentationIdUtility.GetDocumentationId(result.Implementation));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsDecisionRequired_WhenMultipleImplementationsExist()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution) = await LoadWorkspaceAsync();
        var interfaceMethod = FindInterfaceMethod(solution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting");
        var service = new MethodImplementationDecisionService(dbContext);

        var result = await service.ResolveAsync(
            workspace,
            solution,
            Guid.NewGuid(),
            solution.SolutionId,
            interfaceMethod,
            CancellationToken.None);

        Assert.Null(result.Implementation);
        Assert.True(result.DecisionRequired);
        Assert.Contains("SilkHat.Sample.App.FriendlyGreetingProvider", result.CandidateTypeNames);
        Assert.Contains("SilkHat.Sample.App.FormalGreetingProvider", result.CandidateTypeNames);
    }

    [Fact]
    public async Task ResolveAsync_PrefersNonTestImplementation_WhenOnlyOneNonTestCandidate()
    {
        await using var dbContext = DbContextTestFactory.CreateInMemory();
        var (workspace, solution, interfaceMethod) = await BuildCustomWorkspaceAsync();
        var service = new MethodImplementationDecisionService(dbContext);

        var result = await service.ResolveAsync(
            workspace,
            solution,
            Guid.NewGuid(),
            solution.SolutionId,
            interfaceMethod,
            CancellationToken.None);

        Assert.NotNull(result.Implementation);
        Assert.False(result.DecisionRequired);
        Assert.Equal("RealProbe", result.Implementation!.ContainingType.Name);
        Assert.Contains("Samples.RealProbe", result.CandidateTypeNames);
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

    private static INamedTypeSymbol FindNamedType(CodeSolutionWorkspace solution, string typeMetadataName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is not null)
            {
                return type;
            }
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

    private static string BuildInterfaceMethodSignature(IMethodSymbol method)
    {
        var namespaceName = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var typeName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty;
        var methodName = method.MethodKind == MethodKind.Constructor
            ? method.ContainingType?.Name ?? method.Name
            : method.Name;
        var parameters = method.Parameters
            .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var parameterList = string.Join(",", parameters);
        if (string.IsNullOrWhiteSpace(parameterList))
        {
            parameterList = "none";
        }

        return $"{namespaceName}.{typeName}.{methodName}.{parameterList}";
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

    private static async Task<(CodeRepositoryWorkspace Workspace, CodeSolutionWorkspace Solution, IMethodSymbol InterfaceMethod)>
        BuildCustomWorkspaceAsync()
    {
        var appTree = CSharpSyntaxTree.ParseText("""
namespace Samples;

public interface IProbe
{
    string Ping();
}

public sealed class RealProbe : IProbe
{
    public string Ping() => "ok";
}
""");

        var appCompilation = CSharpCompilation.Create(
            "App",
            new[] { appTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        await using var metadataStream = new MemoryStream();
        var emitResult = await appCompilation.EmitAsync(metadataStream, cancellationToken: CancellationToken.None);
        if (!emitResult.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(item => item.ToString()));
            throw new InvalidOperationException($"Failed to emit compilation: {diagnostics}");
        }

        metadataStream.Position = 0;
        var appReference = MetadataReference.CreateFromStream(metadataStream);

        var testTree = CSharpSyntaxTree.ParseText("""
using Samples;

namespace Samples.Tests;

public sealed class FakeProbe : IProbe
{
    public string Ping() => "test";
}
""");

        var testCompilation = CSharpCompilation.Create(
            "App.Tests",
            new[] { testTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                appReference
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var interfaceMethod = appCompilation.GetTypeByMetadataName("Samples.IProbe")!
            .GetMembers()
            .OfType<IMethodSymbol>()
            .First(member => member.Name == "Ping");

        var solution = new CodeSolutionWorkspace(
            "solution-1",
            "CustomSolution",
            "/repos/custom/Custom.sln",
            "./Custom.sln",
            new Dictionary<string, ProjectIndex>
            {
                ["app"] = new ProjectIndex("app", "App", "C#", "App", Array.Empty<CodeProjectReferenceDto>(), Array.Empty<CodeProjectReferenceDto>()),
                ["app.tests"] = new ProjectIndex("app.tests", "App.Tests", "C#", "App.Tests", Array.Empty<CodeProjectReferenceDto>(), Array.Empty<CodeProjectReferenceDto>())
            },
            Array.Empty<CodeTreeEntryDto>(),
            new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(),
            Array.Empty<string>(),
            Array.Empty<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Compilation>
            {
                ["app"] = appCompilation,
                ["app.tests"] = testCompilation
            });

        var workspace = new CodeRepositoryWorkspace("/repos/custom", new Dictionary<string, CodeSolutionWorkspace>
        {
            ["solution-1"] = solution
        });

        return (workspace, solution, interfaceMethod);
    }
}
