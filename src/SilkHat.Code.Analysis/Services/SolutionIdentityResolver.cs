using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class SolutionIdentityResolver
    {
        private readonly SolutionParser _parser = new();

        public string ResolveFromParsedSolution(string rootPath, ParsedSolution solution)
        {
            return SolutionIdentity.Create(rootPath, solution.SolutionPath, solution.SolutionGuid);
        }

        public string ResolveFromRelativePath(string rootPath, string relativePath)
        {
            var fullPath = ResolveSolutionPath(rootPath, relativePath);
            Guid? solutionGuid = null;

            if (File.Exists(fullPath))
            {
                var parsed = _parser.Parse(fullPath);
                solutionGuid = parsed.SolutionGuid;
                fullPath = parsed.SolutionPath;
            }

            return SolutionIdentity.Create(rootPath, fullPath, solutionGuid);
        }

        public static string ResolveSolutionPath(string rootPath, string solutionPath)
        {
            if (Path.IsPathRooted(solutionPath)) return Path.GetFullPath(solutionPath);

            var relative = solutionPath.Trim().Replace('\\', '/').TrimStart('/');
            if (relative.StartsWith("./", StringComparison.Ordinal)) relative = relative[2..];

            return Path.GetFullPath(Path.Combine(rootPath, relative));
        }
    }
}