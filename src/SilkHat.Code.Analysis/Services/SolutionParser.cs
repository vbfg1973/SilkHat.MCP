using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services;

public sealed class SolutionParser
{
    public ParsedSolution Parse(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            throw new ArgumentException("Solution path is required.", nameof(solutionPath));
        }

        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException("Solution file not found.", solutionPath);
        }

        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDirectory))
        {
            throw new InvalidOperationException($"Solution directory could not be resolved for '{solutionPath}'.");
        }

        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        var solutionGuid = FindSolutionGuid(solutionPath);
        var projects = new List<SolutionProject>();
        foreach (var line in File.ReadLines(solutionPath))
        {
            if (!line.StartsWith("Project(", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryParseProjectLine(line, out var projectTypeId, out var name, out var relativePath, out var projectId))
            {
                continue;
            }

            if (!relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedRelativePath = NormalizePath(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, normalizedRelativePath));

            projects.Add(new SolutionProject(
                name,
                normalizedRelativePath,
                fullPath,
                projectTypeId,
                projectId));
        }

        return new ParsedSolution(solutionName, solutionPath, solutionDirectory, solutionGuid, projects);
    }

    private static bool TryParseProjectLine(
        string line,
        out string projectTypeId,
        out string name,
        out string path,
        out string projectId)
    {
        projectTypeId = string.Empty;
        name = string.Empty;
        path = string.Empty;
        projectId = string.Empty;

        var tokens = ExtractQuotedStrings(line);
        if (tokens.Count < 3)
        {
            return false;
        }

        projectTypeId = tokens[0];
        name = tokens[1];
        path = tokens[2];
        if (tokens.Count > 3)
        {
            projectId = tokens[3];
        }

        return true;
    }

    private static List<string> ExtractQuotedStrings(string input)
    {
        var results = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                if (inQuotes)
                {
                    results.Add(new string(current.ToArray()));
                    current.Clear();
                    inQuotes = false;
                }
                else
                {
                    inQuotes = true;
                }

                continue;
            }

            if (inQuotes)
            {
                current.Add(ch);
            }
        }

        return results;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }

    private static Guid? FindSolutionGuid(string solutionPath)
    {
        foreach (var line in File.ReadLines(solutionPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("SolutionGuid", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = trimmed.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var raw = parts[1].Trim().Trim('"', '{', '}');
            if (Guid.TryParse(raw, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
