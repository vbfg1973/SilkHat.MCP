using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeWorkspaceLoader : ICodeWorkspaceLoader
{
    private static readonly IReadOnlyList<MetadataReference> DefaultReferences = BuildDefaultReferences();
    private readonly ILogger<CodeWorkspaceLoader> _logger;
    private readonly SolutionParser _solutionParser = new();
    private readonly ProjectParser _projectParser = new();

    public CodeWorkspaceLoader(ILogger<CodeWorkspaceLoader> logger)
    {
        _logger = logger;
    }

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

        var projects = LoadProjects(resolvedSolutions);
        if (projects.Count == 0)
        {
            throw new InvalidOperationException("No C# projects were found in the selected solutions.");
        }

        var workspace = BuildWorkspace(projects);
        var workspaceProjects = workspace.CurrentSolution.Projects
            .Where(project => !string.IsNullOrWhiteSpace(project.FilePath))
            .ToList();

        var projectKeyMap = workspaceProjects.ToDictionary(p => p.Id, p => p.Id.Id.ToString("N"));
        var projectNameMap = workspaceProjects.ToDictionary(p => p.Id, p => p.Name);
        var projectKeyToName = workspaceProjects.ToDictionary(p => projectKeyMap[p.Id], p => p.Name);

        var compilations = new Dictionary<string, Compilation>();
        foreach (var project in workspaceProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                _logger.LogWarning("Roslyn compilation was null for project {ProjectName}.", project.Name);
                continue;
            }

            compilations[projectKeyMap[project.Id]] = compilation;
        }

        var referenceMap = new Dictionary<string, HashSet<string>>();
        var referencedByMap = new Dictionary<string, HashSet<string>>();

        foreach (var project in workspaceProjects)
        {
            var projectKey = projectKeyMap[project.Id];
            referenceMap[projectKey] = new HashSet<string>();
            referencedByMap[projectKey] = new HashSet<string>();
        }

        foreach (var project in workspaceProjects)
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
        foreach (var project in workspaceProjects)
        {
            var projectKey = projectKeyMap[project.Id];
            var references = referenceMap[projectKey]
                .Select(key => new CodeProjectReferenceDto(
                    key,
                    projectKeyToName.TryGetValue(key, out var name) ? name : key))
                .ToList();
            var referencedBy = referencedByMap[projectKey]
                .Select(key => new CodeProjectReferenceDto(
                    key,
                    projectKeyToName.TryGetValue(key, out var name) ? name : key))
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

        var treeEntries = BuildCodeTreeEntries(workspace, rootPath, projectKeyMap);

        return new CodeRepositoryWorkspace(
            rootPath,
            new List<Workspace> { workspace },
            projectIndex,
            treeEntries,
            namespaces.OrderBy(ns => ns, StringComparer.OrdinalIgnoreCase).ToList(),
            namedTypes,
            namedTypeByKey,
            compilations);
    }

    private Dictionary<string, ParsedProject> LoadProjects(IReadOnlyList<string> solutionPaths)
    {
        var solutions = new Dictionary<string, ParsedSolution>(StringComparer.OrdinalIgnoreCase);
        foreach (var solutionPath in solutionPaths)
        {
            if (!File.Exists(solutionPath))
            {
                throw new InvalidOperationException($"Solution file not found: {solutionPath}");
            }

            solutions[solutionPath] = _solutionParser.Parse(solutionPath);
        }

        var projectsByPath = new Dictionary<string, SolutionProject>(StringComparer.OrdinalIgnoreCase);
        foreach (var solution in solutions.Values)
        {
            foreach (var project in solution.Projects)
            {
                if (!projectsByPath.ContainsKey(project.FullPath))
                {
                    projectsByPath[project.FullPath] = project;
                }
            }
        }

        var parsedProjects = new Dictionary<string, ParsedProject>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projectsByPath.Values)
        {
            if (!File.Exists(project.FullPath))
            {
                throw new InvalidOperationException($"Project file not found: {project.FullPath}");
            }

            parsedProjects[project.FullPath] = _projectParser.Parse(project.FullPath);
        }

        return parsedProjects;
    }

    private static Workspace BuildWorkspace(IReadOnlyDictionary<string, ParsedProject> projects)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var projectIds = projects.ToDictionary(entry => entry.Key, _ => ProjectId.CreateNewId(), StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Values)
        {
            var projectId = projectIds[project.ProjectPath];
            var projectInfo = CreateProjectInfo(project, projectId);
            solution = solution.AddProject(projectInfo);
        }

        foreach (var project in projects.Values)
        {
            var projectId = projectIds[project.ProjectPath];
            foreach (var document in CreateDocuments(projectId, project))
            {
                solution = solution.AddDocument(document);
            }

            foreach (var reference in project.ProjectReferences)
            {
                if (!projectIds.TryGetValue(reference.FullPath, out var referencedId))
                {
                    continue;
                }

                solution = solution.AddProjectReference(projectId, new ProjectReference(referencedId));
            }
        }

        workspace.TryApplyChanges(solution);
        return workspace;
    }

    private static ProjectInfo CreateProjectInfo(ParsedProject project, ProjectId projectId)
    {
        var parseOptions = CreateParseOptions(project);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        return ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                project.ProjectName,
                project.AssemblyName,
                LanguageNames.CSharp,
                filePath: project.ProjectPath)
            .WithCompilationOptions(compilationOptions)
            .WithParseOptions(parseOptions)
            .WithMetadataReferences(DefaultReferences);
    }

    private static IEnumerable<DocumentInfo> CreateDocuments(ProjectId projectId, ParsedProject project)
    {
        foreach (var filePath in project.CompileItems)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            var name = Path.GetFileName(filePath);
            var text = SourceText.From(File.ReadAllText(filePath));
            var loader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), filePath));
            yield return DocumentInfo.Create(DocumentId.CreateNewId(projectId), name, filePath: filePath, loader: loader);
        }
    }

    private static CSharpParseOptions CreateParseOptions(ParsedProject project)
    {
        var languageVersion = LanguageVersion.Latest;
        if (!string.IsNullOrWhiteSpace(project.LangVersion)
            && LanguageVersionFacts.TryParse(project.LangVersion, out var parsedVersion))
        {
            languageVersion = parsedVersion;
        }

        return new CSharpParseOptions(languageVersion, preprocessorSymbols: project.DefineConstants);
    }

    private static IReadOnlyList<MetadataReference> BuildDefaultReferences()
    {
        var references = new List<MetadataReference>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seedAssemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Task).Assembly
        };

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Concat(seedAssemblies))
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            var location = assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            if (paths.Add(location))
            {
                references.Add(MetadataReference.CreateFromFile(location));
            }
        }

        return references;
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

    private static NamedTypeDto ToNamedTypeDto(
        INamedTypeSymbol symbol,
        Compilation compilation,
        string projectKey,
        string rootPath)
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

    private static IReadOnlyList<CodeTreeEntryDto> BuildCodeTreeEntries(
        Workspace workspace,
        string rootPath,
        IReadOnlyDictionary<ProjectId, string> projectKeyMap)
    {
        var entries = new List<CodeTreeEntryDto>();
        var entryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in workspace.CurrentSolution.Projects)
        {
            if (string.IsNullOrWhiteSpace(project.FilePath))
            {
                continue;
            }

            if (!projectKeyMap.TryGetValue(project.Id, out var projectKey))
            {
                continue;
            }

            var projectRoot = Path.GetDirectoryName(project.FilePath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                continue;
            }

            var projectRootRepoPath = NormalizeRelativePath(rootPath, projectRoot);
            var projectDisplayPath = project.Name;

            AddEntry(
                entries,
                entryKeys,
                projectRootRepoPath,
                projectDisplayPath,
                project.Name,
                CodeTreeEntryType.Project,
                projectKey,
                project.Name);

            var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var document in project.Documents)
            {
                if (string.IsNullOrWhiteSpace(document.FilePath))
                {
                    continue;
                }

                var relativeToProject = NormalizePathKey(Path.GetRelativePath(projectRoot, document.FilePath));
                if (relativeToProject.StartsWith("..", StringComparison.Ordinal))
                {
                    continue;
                }

                var repositoryPath = NormalizeRelativePath(rootPath, document.FilePath);
                var displayPath = $"{projectDisplayPath}/{relativeToProject}";
                AddEntry(
                    entries,
                    entryKeys,
                    repositoryPath,
                    displayPath,
                    Path.GetFileName(document.FilePath),
                    CodeTreeEntryType.File,
                    projectKey,
                    project.Name);

                var folder = Path.GetDirectoryName(relativeToProject);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                var parts = NormalizePathKey(folder).Split('/', StringSplitOptions.RemoveEmptyEntries);
                var current = string.Empty;
                foreach (var part in parts)
                {
                    current = string.IsNullOrEmpty(current) ? part : $"{current}/{part}";
                    directories.Add(current);
                }
            }

            foreach (var directory in directories.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                var directoryFullPath = Path.Combine(projectRoot, directory.Replace('/', Path.DirectorySeparatorChar));
                var repositoryPath = NormalizeRelativePath(rootPath, directoryFullPath);
                var displayPath = $"{projectDisplayPath}/{directory}";
                AddEntry(
                    entries,
                    entryKeys,
                    repositoryPath,
                    displayPath,
                    Path.GetFileName(directory),
                    CodeTreeEntryType.Directory,
                    projectKey,
                    project.Name);
            }
        }

        return entries
            .OrderBy(entry => entry.DisplayPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddEntry(
        ICollection<CodeTreeEntryDto> entries,
        ISet<string> keys,
        string repositoryPath,
        string displayPath,
        string name,
        CodeTreeEntryType type,
        string projectKey,
        string projectName)
    {
        var key = $"{type}:{repositoryPath}:{projectKey}";
        if (!keys.Add(key))
        {
            return;
        }

        entries.Add(new CodeTreeEntryDto(
            repositoryPath,
            displayPath,
            name,
            type,
            projectKey,
            projectName));
    }

    private static string NormalizePathKey(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }
}
