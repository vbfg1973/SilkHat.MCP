using Microsoft.Extensions.Options;
using SilkHat.Analysis.Models;
using SilkHat.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class RepositoryDiscoveryServiceTests
{
    [Fact]
    public void ListAvailableRepositories_ReturnsDirectoriesWithGitFlag()
    {
        var root = CreateTempDirectory();
        try
        {
            var gitRepo = Path.Combine(root, "RepoA");
            Directory.CreateDirectory(Path.Combine(gitRepo, ".git"));
            var plainRepo = Path.Combine(root, "RepoB");
            Directory.CreateDirectory(plainRepo);

            var service = CreateService(root);

            var results = service.ListAvailableRepositories();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, item => item.Name == "RepoA" && item.IsGitRepository);
            Assert.Contains(results, item => item.Name == "RepoB" && !item.IsGitRepository);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void TryValidateRepositoryPath_ReturnsFalse_WhenOutsideRoot()
    {
        var root = CreateTempDirectory();
        var other = CreateTempDirectory();
        try
        {
            var service = CreateService(root);
            var valid = service.TryValidateRepositoryPath(other, out var error);

            Assert.False(valid);
            Assert.NotNull(error);
        }
        finally
        {
            Directory.Delete(root, true);
            Directory.Delete(other, true);
        }
    }

    [Fact]
    public void TryValidateRepositoryPath_ReturnsFalse_WhenNotGit()
    {
        var root = CreateTempDirectory();
        try
        {
            var repo = Path.Combine(root, "RepoA");
            Directory.CreateDirectory(repo);
            var service = CreateService(root);

            var valid = service.TryValidateRepositoryPath(repo, out var error);

            Assert.False(valid);
            Assert.NotNull(error);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void TryValidateRepositoryPath_ReturnsTrue_ForGitRepo()
    {
        var root = CreateTempDirectory();
        try
        {
            var repo = Path.Combine(root, "RepoA");
            Directory.CreateDirectory(Path.Combine(repo, ".git"));
            var service = CreateService(root);

            var valid = service.TryValidateRepositoryPath(repo, out var error);

            Assert.True(valid);
            Assert.Null(error);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ListSolutions_ReturnsRepoRelativePaths()
    {
        var root = CreateTempDirectory();
        try
        {
            var repo = Path.Combine(root, "RepoA");
            Directory.CreateDirectory(Path.Combine(repo, ".git"));
            var solutionDir = Path.Combine(repo, "src");
            Directory.CreateDirectory(solutionDir);
            var slnPath = Path.Combine(solutionDir, "RepoA.sln");
            File.WriteAllText(slnPath, "sln");

            var service = CreateService(root);

            var solutions = service.ListSolutions(repo);

            Assert.Single(solutions);
            Assert.Equal("./src/RepoA.sln", solutions[0].RelativePath);
            Assert.False(string.IsNullOrWhiteSpace(solutions[0].SolutionId));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static RepositoryDiscoveryService CreateService(string root)
    {
        var options = Options.Create(new RepositoryDiscoveryOptions { RepoRoot = root });
        return new RepositoryDiscoveryService(options);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "silkhat-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
