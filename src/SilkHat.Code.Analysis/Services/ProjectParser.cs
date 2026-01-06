using System.Xml.Linq;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services;

public sealed class ProjectParser
{
    public ParsedProject Parse(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        }

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException("Project file not found.", projectPath);
        }

        var document = XDocument.Load(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var projectName = Path.GetFileNameWithoutExtension(projectPath);

        var targetFrameworks = SplitSemicolonList(GetPropertyValue(document, "TargetFrameworks"))
            .Concat(SplitSemicolonList(GetPropertyValue(document, "TargetFramework")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var defineConstants = SplitSemicolonList(GetPropertyValue(document, "DefineConstants"))
            .Where(value => !string.IsNullOrWhiteSpace(value) && !value.Contains("$(", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var assemblyName = GetPropertyValue(document, "AssemblyName");
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = projectName;
        }

        var rootNamespace = GetPropertyValue(document, "RootNamespace");
        var langVersion = GetPropertyValue(document, "LangVersion");
        var enableDefaultCompileItems = !string.Equals(
            GetPropertyValue(document, "EnableDefaultCompileItems"),
            "false",
            StringComparison.OrdinalIgnoreCase);

        var projectReferences = new List<ProjectReferenceInfo>();
        var packageReferences = new List<PackageReferenceInfo>();
        var compileIncludes = new List<string>();
        var compileRemoves = new List<string>();

        foreach (var element in document.Descendants())
        {
            var localName = element.Name.LocalName;
            if (string.Equals(localName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            {
                var include = GetAttributeValue(element, "Include");
                if (string.IsNullOrWhiteSpace(include))
                {
                    continue;
                }

                var includePath = NormalizePath(include);
                var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, includePath));
                projectReferences.Add(new ProjectReferenceInfo(includePath, fullPath));
            }
            else if (string.Equals(localName, "PackageReference", StringComparison.OrdinalIgnoreCase))
            {
                var id = GetAttributeValue(element, "Include") ?? GetAttributeValue(element, "Update");
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var version = GetAttributeValue(element, "Version") ?? GetElementValue(element, "Version");
                packageReferences.Add(new PackageReferenceInfo(id, string.IsNullOrWhiteSpace(version) ? null : version));
            }
            else if (string.Equals(localName, "Compile", StringComparison.OrdinalIgnoreCase))
            {
                var include = GetAttributeValue(element, "Include") ?? GetAttributeValue(element, "Update");
                var remove = GetAttributeValue(element, "Remove");
                var exclude = GetAttributeValue(element, "Exclude");

                compileIncludes.AddRange(SplitSemicolonList(include));
                compileRemoves.AddRange(SplitSemicolonList(remove));
                compileRemoves.AddRange(SplitSemicolonList(exclude));
            }
        }

        var compileItems = ResolveCompileItems(
            projectDirectory,
            enableDefaultCompileItems,
            compileIncludes,
            compileRemoves);

        return new ParsedProject(
            projectPath,
            projectName,
            assemblyName,
            string.IsNullOrWhiteSpace(rootNamespace) ? null : rootNamespace,
            targetFrameworks,
            defineConstants,
            string.IsNullOrWhiteSpace(langVersion) ? null : langVersion,
            enableDefaultCompileItems,
            projectReferences,
            packageReferences,
            compileItems);
    }

    private static string? GetPropertyValue(XDocument document, string name)
    {
        foreach (var propertyGroup in document.Descendants().Where(element => element.Name.LocalName == "PropertyGroup"))
        {
            var match = propertyGroup.Elements().FirstOrDefault(element => element.Name.LocalName == name);
            if (match is null)
            {
                continue;
            }

            var value = match.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetAttributeValue(XElement element, string name)
        => element.Attributes().FirstOrDefault(attribute =>
            string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value?.Trim();

    private static string? GetElementValue(XElement element, string name)
        => element.Elements().FirstOrDefault(child =>
            string.Equals(child.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value?.Trim();

    private static IReadOnlyList<string> ResolveCompileItems(
        string projectDirectory,
        bool enableDefaultCompileItems,
        IReadOnlyList<string> includePatterns,
        IReadOnlyList<string> removePatterns)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (enableDefaultCompileItems)
        {
            foreach (var file in EnumerateSourceFiles(projectDirectory))
            {
                candidates.Add(file);
            }
        }

        foreach (var pattern in includePatterns)
        {
            foreach (var file in ExpandPattern(projectDirectory, pattern))
            {
                candidates.Add(file);
            }
        }

        foreach (var pattern in removePatterns)
        {
            foreach (var file in ExpandPattern(projectDirectory, pattern))
            {
                candidates.Remove(file);
            }
        }

        return candidates
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> EnumerateSourceFiles(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path));
    }

    private static IEnumerable<string> ExpandPattern(string rootDirectory, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Array.Empty<string>();
        }

        var normalized = NormalizePath(pattern);
        if (!normalized.Contains('*') && !normalized.Contains('?'))
        {
            var full = Path.GetFullPath(Path.Combine(rootDirectory, normalized));
            return File.Exists(full) ? new[] { full } : Array.Empty<string>();
        }

        var regex = GlobToRegex(normalized);
        return EnumerateSourceFiles(rootDirectory)
            .Where(path =>
            {
                var relative = NormalizePath(Path.GetRelativePath(rootDirectory, path));
                return regex.IsMatch(relative);
            });
    }

    private static System.Text.RegularExpressions.Regex GlobToRegex(string pattern)
    {
        var normalized = NormalizePath(pattern);
        var escaped = System.Text.RegularExpressions.Regex.Escape(normalized);
        escaped = escaped.Replace(@"\*\*", ".*", StringComparison.Ordinal);
        escaped = escaped.Replace(@"\*", "[^/]*", StringComparison.Ordinal);
        escaped = escaped.Replace(@"\?", ".", StringComparison.Ordinal);
        return new System.Text.RegularExpressions.Regex($"^{escaped}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsIgnoredPath(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("/.vs/", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SplitSemicolonList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry));
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
}
