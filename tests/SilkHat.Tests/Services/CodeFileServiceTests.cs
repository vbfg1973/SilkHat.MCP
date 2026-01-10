using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class CodeFileServiceTests
{
    [Fact]
    public async Task GetFileAsync_ReturnsContent_ForKnownFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "Program.cs");
        await File.WriteAllTextAsync(filePath, "class Program {}");
        try
        {
            var service = new CodeFileService();
            var solution = BuildSolution("./Program.cs", "Repo/Program.cs");
            var workspace = new CodeRepositoryWorkspace(root, new Dictionary<string, CodeSolutionWorkspace>());

            var result = await service.GetFileAsync(workspace, solution, "Repo/Program.cs", CancellationToken.None);

            Assert.Equal(CodeFileContentStatus.Success, result.Status);
            Assert.NotNull(result.Content);
            Assert.Equal("./Program.cs", result.Content!.RepositoryPath);
            Assert.Equal("Repo/Program.cs", result.Content.DisplayPath);
            Assert.Contains("class Program", result.Content.Content);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task GetFileAsync_ReturnsNotFound_WhenDisplayPathMissing()
    {
        var service = new CodeFileService();
        var solution = BuildSolution("./Program.cs", "Repo/Program.cs");
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>());

        var result = await service.GetFileAsync(workspace, solution, "Repo/Other.cs", CancellationToken.None);

        Assert.Equal(CodeFileContentStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetFileAsync_ReturnsInvalidPath_WhenTraversalDetected()
    {
        var service = new CodeFileService();
        var solution = BuildSolution("./../secrets.txt", "Repo/secrets.txt");
        var workspace = new CodeRepositoryWorkspace("/repo", new Dictionary<string, CodeSolutionWorkspace>());

        var result = await service.GetFileAsync(workspace, solution, "Repo/secrets.txt", CancellationToken.None);

        Assert.Equal(CodeFileContentStatus.InvalidPath, result.Status);
    }

    private static CodeSolutionWorkspace BuildSolution(string repositoryPath, string displayPath)
    {
        var entry = new CodeTreeEntryDto(
            repositoryPath,
            displayPath,
            Path.GetFileName(displayPath),
            CodeTreeEntryType.File,
            "alpha",
            "Alpha",
            null,
            null,
            null,
            null,
            null,
            null);
        return new CodeSolutionWorkspace(
            "solution-1",
            "Repo",
            "/repo/Repo.sln",
            "./Repo.sln",
            new Dictionary<string, ProjectIndex>(),
            new List<CodeTreeEntryDto> { entry },
            new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
            new List<string>(),
            new List<NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, NamedTypeDto>(),
            new Dictionary<string, Microsoft.CodeAnalysis.Compilation>());
    }
}
