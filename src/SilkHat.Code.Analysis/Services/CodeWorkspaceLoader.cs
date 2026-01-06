using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeWorkspaceLoader : ICodeWorkspaceLoader
{
    public async Task<CodeRepositoryWorkspace> LoadAsync(
        string rootPath,
        IReadOnlyList<string> solutionPaths,
        CancellationToken cancellationToken)
    {
        if (solutionPaths is null || solutionPaths.Count == 0)
        {
            throw new InvalidOperationException("No solution files selected for loading.");
        }

        var resolvedSolutions = solutionPaths
            .Select(path => ResolveSolutionPath(rootPath, path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resolvedSolutions.Count == 0)
        {
            throw new InvalidOperationException("No solution files selected for loading.");
        }

        var workspaces = new List<Workspace>();
        var projectsByPath = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);

        foreach (var solutionPath in resolvedSolutions)
        {
            if (!File.Exists(solutionPath))
            {
                throw new InvalidOperationException($"Solution file not found: {solutionPath}");
            }

            var manager = new AnalyzerManager(solutionPath);
            var workspace = new AdhocWorkspace();

            foreach (var analyzer in manager.Projects.Values)
            {
                analyzer.AddToWorkspace(workspace);
            }

            workspaces.Add(workspace);

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                if (string.IsNullOrWhiteSpace(project.FilePath))
                {
                    continue;
                }

                projectsByPath[project.FilePath] = project;
            }
        }

        var projects = projectsByPath.Values.ToList();
        var projectKeyMap = projects.ToDictionary(p => p.Id, p => p.Id.Id.ToString("N"));
        var projectNameMap = projects.ToDictionary(p => p.Id, p => p.Name);
        var projectKeyToName = projects.ToDictionary(p => projectKeyMap[p.Id], p => p.Name);

        var compilations = new Dictionary<string, Compilation>();
        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            compilations[projectKeyMap[project.Id]] = compilation;
        }

        var referenceMap = new Dictionary<string, HashSet<string>>();
        var referencedByMap = new Dictionary<string, HashSet<string>>();

        foreach (var project in projects)
        {
            var projectKey = projectKeyMap[project.Id];
            referenceMap[projectKey] = new HashSet<string>();
            referencedByMap[projectKey] = new HashSet<string>();
        }

        foreach (var project in projects)
        {
            var projectKey = projectKeyMap[project.Id];
            foreach (var reference in project.ProjectReferences)
            {
                if (!projectKeyMap.TryGetValue(reference.ProjectId, out var referencedKey))
                {
                    continue;
                }

                referenceMap[projectKey].Add(referencedKey);
                referencedByMap[referencedKey].Add(projectKey);
            }
        }

        var projectIndex = new Dictionary<string, ProjectIndex>();
        foreach (var project in projects)
        {
            var projectKey = projectKeyMap[project.Id];
            var references = referenceMap[projectKey]
                .Select(key => new CodeProjectReferenceDto(key, projectKeyToName.TryGetValue(key, out var name) ? name : key))
                .ToList();
            var referencedBy = referencedByMap[projectKey]
                .Select(key => new CodeProjectReferenceDto(key, projectKeyToName.TryGetValue(key, out var name) ? name : key))
                .ToList();

            projectIndex[projectKey] = new ProjectIndex(
                projectKey,
                project.Name,
                project.Language,
                project.AssemblyName ?? project.Name,
                references,
                referencedBy);
        }

        var namedTypes = new List<NamedTypeDto>();
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var namedTypeByKey = new Dictionary<string, NamedTypeDto>(StringComparer.Ordinal);

        foreach (var compilationEntry in compilations)
        {
            var projectKey = compilationEntry.Key;
            var compilation = compilationEntry.Value;
            foreach (var symbol in EnumerateNamedTypes(compilation.GlobalNamespace))
            {
                var dto = ToNamedTypeDto(symbol, compilation, projectKey, rootPath);
                namedTypes.Add(dto);
                if (!string.IsNullOrWhiteSpace(dto.Namespace))
                {
                    namespaces.Add(dto.Namespace);
                }

                if (!namedTypeByKey.ContainsKey(dto.SymbolKey))
                {
                    namedTypeByKey[dto.SymbolKey] = dto;
                }
            }
        }

        return new CodeRepositoryWorkspace(
            rootPath,
            workspaces,
            projectIndex,
            namespaces.OrderBy(ns => ns, StringComparer.OrdinalIgnoreCase).ToList(),
            namedTypes,
            namedTypeByKey,
            compilations);
    }

    private static string ResolveSolutionPath(string rootPath, string solutionPath)
    {
        if (Path.IsPathRooted(solutionPath))
        {
            return Path.GetFullPath(solutionPath);
        }

        var relative = solutionPath.Trim().Replace('\\', '/').TrimStart('/');
        if (relative.StartsWith("./", StringComparison.Ordinal))
        {
            relative = relative[2..];
        }

        return Path.GetFullPath(Path.Combine(rootPath, relative));
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                foreach (var nested in EnumerateNamedTypes(ns))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in EnumerateNestedTypes(type))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deeper in EnumerateNestedTypes(nested))
            {
                yield return deeper;
            }
        }
    }

    private static NamedTypeDto ToNamedTypeDto(INamedTypeSymbol symbol, Compilation compilation, string projectKey, string rootPath)
    {
        var kind = symbol.IsRecord
            ? NamedTypeKind.Record
            : symbol.TypeKind switch
            {
                TypeKind.Class => NamedTypeKind.Class,
                TypeKind.Struct => NamedTypeKind.Struct,
                TypeKind.Interface => NamedTypeKind.Interface,
                TypeKind.Enum => NamedTypeKind.Enum,
                TypeKind.Delegate => NamedTypeKind.Delegate,
                _ => NamedTypeKind.Unknown
            };

        var name = symbol.Name;
        var ns = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var fullName = symbol.ToDisplayString();
        var assemblyName = symbol.ContainingAssembly?.Name ?? string.Empty;

        string? filePath = null;
        var sourceLocation = symbol.Locations.FirstOrDefault(location => location.IsInSource);
        if (sourceLocation?.SourceTree?.FilePath is { Length: > 0 } sourcePath)
        {
            filePath = NormalizeRelativePath(rootPath, sourcePath);
        }

        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(symbol, compilation);
        var isExternal = !symbol.Locations.Any(location => location.IsInSource);

        return new NamedTypeDto(
            symbolKey,
            name,
            ns,
            fullName,
            projectKey,
            assemblyName,
            kind,
            isExternal,
            filePath);
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
}
