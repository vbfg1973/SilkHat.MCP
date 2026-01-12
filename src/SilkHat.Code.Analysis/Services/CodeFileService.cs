using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class CodeFileService : ICodeFileService
    {
        public async Task<CodeFileContentResult> GetFileAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            string displayPath,
            CancellationToken cancellationToken)
        {
            if (workspace is null) throw new ArgumentNullException(nameof(workspace));

            if (solution is null) throw new ArgumentNullException(nameof(solution));

            if (string.IsNullOrWhiteSpace(displayPath))
                return new CodeFileContentResult(
                    CodeFileContentStatus.InvalidPath,
                    null,
                    "Path cannot be empty.");

            var entry = solution.TreeEntries.FirstOrDefault(item =>
                item.Type == CodeTreeEntryType.File &&
                (string.Equals(item.DisplayPath, displayPath, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(item.RepositoryPath, displayPath, StringComparison.OrdinalIgnoreCase)));
            if (entry is null)
                return new CodeFileContentResult(
                    CodeFileContentStatus.NotFound,
                    null,
                    "File not found in solution.");

            var fullPathResult = TryResolvePath(workspace.RootPath, entry.RepositoryPath, out var fullPath);
            if (!fullPathResult)
                return new CodeFileContentResult(
                    CodeFileContentStatus.InvalidPath,
                    null,
                    "Invalid file path.");

            if (!File.Exists(fullPath))
                return new CodeFileContentResult(
                    CodeFileContentStatus.NotFound,
                    null,
                    "File not found on disk.");

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var dto = new CodeFileContentDto(entry.RepositoryPath, entry.DisplayPath, content);
            return new CodeFileContentResult(CodeFileContentStatus.Success, dto, null);
        }

        private static bool TryResolvePath(string repositoryRoot, string repositoryPath, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(repositoryPath)) return false;

            var rootFullPath = Path.GetFullPath(repositoryRoot);
            var relativePath = repositoryPath.Replace('\\', '/');
            if (relativePath.StartsWith("./", StringComparison.Ordinal)) relativePath = relativePath[2..];

            relativePath = relativePath.TrimStart('/');
            var combined = Path.Combine(rootFullPath, relativePath);
            var resolved = Path.GetFullPath(combined);
            var relative = Path.GetRelativePath(rootFullPath, resolved);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) return false;

            fullPath = resolved;
            return true;
        }
    }
}