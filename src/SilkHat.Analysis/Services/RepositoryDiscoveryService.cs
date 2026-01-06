using Microsoft.Extensions.Options;
using System.Linq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Services;

public sealed class RepositoryDiscoveryService : IRepositoryDiscoveryService
{
    private readonly string? _repoRoot;

    public RepositoryDiscoveryService(IOptions<RepositoryDiscoveryOptions> options)
    {
        _repoRoot = NormalizeRoot(options.Value.RepoRoot);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_repoRoot);

    public string? RepositoryRoot => _repoRoot;

    public IReadOnlyList<AvailableRepositoryDto> ListAvailableRepositories()
    {
        EnsureConfigured();

        if (!Directory.Exists(_repoRoot!))
        {
            throw new InvalidOperationException($"Repository root '{_repoRoot}' does not exist.");
        }

        var results = new List<AvailableRepositoryDto>();
        foreach (var directory in Directory.EnumerateDirectories(_repoRoot!))
        {
            var name = Path.GetFileName(directory);
            var relative = Path.GetRelativePath(_repoRoot!, directory);
            results.Add(new AvailableRepositoryDto(
                name,
                relative.Replace('\\', '/'),
                directory,
                IsGitRepository(directory)));
        }

        return results
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<AvailableRepositorySolutionDto> ListSolutions(string rootPath)
    {
        EnsureConfigured();

        var fullPath = NormalizeRoot(rootPath);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new InvalidOperationException("RootPath is invalid.");
        }

        if (!Directory.Exists(fullPath))
        {
            throw new InvalidOperationException("RootPath does not exist.");
        }

        if (!IsUnderRoot(fullPath, _repoRoot!))
        {
            throw new InvalidOperationException("RootPath must be under the configured repository root.");
        }

        if (!IsGitRepository(fullPath))
        {
            throw new InvalidOperationException("RootPath is not a git repository.");
        }

        var solutions = Directory.EnumerateFiles(fullPath, "*.sln", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new AvailableRepositorySolutionDto(NormalizeRelativePath(fullPath, path)))
            .ToList();

        return solutions;
    }

    public bool TryValidateRepositoryPath(string rootPath, out string? error)
    {
        error = null;

        if (!IsConfigured)
        {
            error = "Repository discovery is not configured. Set REPO_ROOT.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            error = "RootPath is required.";
            return false;
        }

        var fullPath = NormalizeRoot(rootPath);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            error = "RootPath is invalid.";
            return false;
        }

        if (!Directory.Exists(fullPath))
        {
            error = "RootPath does not exist.";
            return false;
        }

        if (!IsUnderRoot(fullPath, _repoRoot!))
        {
            error = "RootPath must be under the configured repository root.";
            return false;
        }

        if (!IsGitRepository(fullPath))
        {
            error = "RootPath is not a git repository.";
            return false;
        }

        return true;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Repository discovery is not configured. Set REPO_ROOT.");
        }
    }

    private static string? NormalizeRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetFullPath(path.Trim());
    }

    private static string NormalizeRelativePath(string rootPath, string fullPath)
    {
        var relative = Path.GetRelativePath(rootPath, fullPath);
        relative = relative.Replace('\\', '/');
        if (!relative.StartsWith(".", StringComparison.Ordinal))
        {
            relative = "./" + relative;
        }

        return relative;
    }

    private static bool IsUnderRoot(string candidate, string root)
    {
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
               || string.Equals(candidate, root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGitRepository(string path)
    {
        var gitDir = Path.Combine(path, ".git");
        return Directory.Exists(gitDir) || File.Exists(gitDir);
    }
}
