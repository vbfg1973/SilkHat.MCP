using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using System.Security.Cryptography;
using System.Text;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeWorkspaceLoader : ICodeWorkspaceLoader
{
    private static readonly IReadOnlyList<MetadataReference> DefaultReferences = BuildDefaultReferences();
    private readonly ILogger<CodeWorkspaceLoader> _logger;
    private readonly IGraphStoreProvider _graphStoreProvider;
    private readonly SolutionParser _solutionParser = new();
    private readonly ProjectParser _projectParser = new();
    private readonly SolutionIdentityResolver _identityResolver = new();

    public CodeWorkspaceLoader(
        ILogger<CodeWorkspaceLoader> logger,
        IGraphStoreProvider? graphStoreProvider = null)
    {
        _logger = logger;
        _graphStoreProvider = graphStoreProvider ?? new GraphStoreProvider();
    }

    public async Task<CodeRepositoryWorkspace> LoadAsync(
        string rootPath,
        IReadOnlyList<SolutionReference> solutions,
        CancellationToken cancellationToken)
    {
        if (solutions is null || solutions.Count == 0)
        {
            throw new InvalidOperationException("No solution files selected for loading.");
        }

        var resolvedSolutions = solutions
            .Where(solution => !string.IsNullOrWhiteSpace(solution.RelativePath))
            .Select(solution => new
            {
                Reference = solution,
                FullPath = SolutionIdentityResolver.ResolveSolutionPath(rootPath, solution.RelativePath)
            })
            .GroupBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                if (group.Any(item => !string.Equals(item.Reference.SolutionId, first.Reference.SolutionId, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Multiple solutionIds provided for '{first.FullPath}'.");
                }

                return first;
            })
            .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resolvedSolutions.Count == 0)
        {
            throw new InvalidOperationException("No solution files selected for loading.");
        }

        var solutionWorkspaces = new Dictionary<string, CodeSolutionWorkspace>(StringComparer.OrdinalIgnoreCase);

        foreach (var solutionEntry in resolvedSolutions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var solutionPath = solutionEntry.FullPath;
            if (!File.Exists(solutionPath))
            {
                throw new InvalidOperationException($"Solution file not found: {solutionPath}");
            }

            var parsedSolution = _solutionParser.Parse(solutionPath);
            var solutionId = !string.IsNullOrWhiteSpace(solutionEntry.Reference.SolutionId)
                ? solutionEntry.Reference.SolutionId
                : _identityResolver.ResolveFromParsedSolution(rootPath, parsedSolution);
            var solutionProjects = LoadProjectsForSolution(parsedSolution);
            if (solutionProjects.Count == 0)
            {
                throw new InvalidOperationException($"No C# projects were found in solution '{parsedSolution.SolutionPath}'.");
            }

            var workspace = BuildWorkspace(solutionProjects);
            var workspaceProjects = workspace.CurrentSolution.Projects
                .Where(project => !string.IsNullOrWhiteSpace(project.FilePath))
                .ToList();

            var projectKeyMap = workspaceProjects.ToDictionary(p => p.Id, p => p.Id.Id.ToString("N"));
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
            var namedTypeByDocId = new Dictionary<string, NamedTypeDto>(StringComparer.Ordinal);

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

                    if (!string.IsNullOrWhiteSpace(dto.DocumentationId)
                        && !namedTypeByDocId.ContainsKey(dto.DocumentationId))
                    {
                        namedTypeByDocId[dto.DocumentationId] = dto;
                    }
                }
            }

            var treeEntries = BuildCodeTreeEntries(workspace, rootPath, projectKeyMap);
            var treeChildrenMap = await Task.Run(() => BuildTreeChildrenMap(treeEntries), cancellationToken);
            var relativeSolutionPath = SolutionIdentity.NormalizeRelativePath(rootPath, parsedSolution.SolutionPath);

            solutionWorkspaces[solutionId] = new CodeSolutionWorkspace(
                solutionId,
                parsedSolution.SolutionName,
                parsedSolution.SolutionPath,
                relativeSolutionPath,
                projectIndex,
                treeEntries,
                treeChildrenMap,
                namespaces.OrderBy(ns => ns, StringComparer.OrdinalIgnoreCase).ToList(),
                namedTypes,
                namedTypeByKey,
                namedTypeByDocId,
                compilations);

            PopulateGraph(
                solutionId,
                parsedSolution.SolutionName,
                projectIndex,
                treeEntries,
                treeChildrenMap,
                namedTypes);
        }

        return new CodeRepositoryWorkspace(rootPath, solutionWorkspaces);
    }

    private void PopulateGraph(
        string solutionId,
        string solutionName,
        IReadOnlyDictionary<string, ProjectIndex> projects,
        IReadOnlyList<CodeTreeEntryDto> treeEntries,
        IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> treeChildrenByParent,
        IReadOnlyList<NamedTypeDto> namedTypes)
    {
        var graph = _graphStoreProvider.GetOrAdd(solutionId);

        var solutionNodeId = CreateStableId($"solution:{solutionId}");
        graph.AddNode(new GraphNodeDto(solutionNodeId, GraphNodeKind.Solution, solutionId, solutionName));

        var displayPathToNodeId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var repositoryPathToNodeId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Values)
        {
            var projectNodeId = CreateStableId($"project:{project.ProjectKey}");
            graph.AddNode(new GraphNodeDto(projectNodeId, GraphNodeKind.Project, project.ProjectKey, project.Name));
            graph.AddEdge(solutionNodeId, projectNodeId, EdgeType.Contains);
            displayPathToNodeId[project.Name] = projectNodeId;
        }

        foreach (var entry in treeEntries)
        {
            var kind = entry.Type switch
            {
                CodeTreeEntryType.Project => GraphNodeKind.Project,
                CodeTreeEntryType.Directory => GraphNodeKind.Folder,
                CodeTreeEntryType.File => GraphNodeKind.File,
                _ => GraphNodeKind.File
            };

            var nodeId = CreateStableId($"{entry.Type}:{entry.DisplayPath}:{entry.ProjectKey}");
            var attributes = new Dictionary<string, string>
            {
                ["DisplayPath"] = entry.DisplayPath
            };
            if (entry.Type == CodeTreeEntryType.File)
            {
                attributes["RepositoryPath"] = entry.RepositoryPath;
            }

            graph.AddNode(new GraphNodeDto(nodeId, kind, entry.DisplayPath, entry.Name, attributes));
            displayPathToNodeId[entry.DisplayPath] = nodeId;
            if (entry.Type == CodeTreeEntryType.File)
            {
                repositoryPathToNodeId[entry.RepositoryPath] = nodeId;
            }
        }

        foreach (var (parent, children) in treeChildrenByParent)
        {
            var hasParent = displayPathToNodeId.TryGetValue(parent, out var parentNodeId);
            // root-level entries hang directly off the solution
            var rootParentId = hasParent ? parentNodeId : solutionNodeId;

            foreach (var child in children)
            {
                if (!displayPathToNodeId.TryGetValue(child.DisplayPath, out var childNodeId))
                {
                    continue;
                }

                graph.AddEdge(rootParentId, childNodeId, EdgeType.Contains);
            }
        }

        foreach (var type in namedTypes)
        {
            var typeKey = !string.IsNullOrWhiteSpace(type.DocumentationId)
                ? type.DocumentationId
                : type.SymbolKey;
            var typeNodeId = CreateStableId($"type:{typeKey}");
            var attributes = new Dictionary<string, string>
            {
                ["DocumentationId"] = type.DocumentationId ?? string.Empty,
                ["SymbolKey"] = type.SymbolKey,
                ["RealType"] = type.Kind.ToString(),
                ["Name"] = type.Name
            };
            graph.AddNode(new GraphNodeDto(typeNodeId, GraphNodeKind.NamedType, typeKey, type.Name, attributes));

            if (!string.IsNullOrWhiteSpace(type.FilePath) &&
                repositoryPathToNodeId.TryGetValue(type.FilePath, out var fileNodeId))
            {
                graph.AddEdge(fileNodeId, typeNodeId, EdgeType.DeclaresType);
            }
        }
    }

    private Dictionary<string, ParsedProject> LoadProjectsForSolution(ParsedSolution solution)
    {
        var projectsByPath = new Dictionary<string, SolutionProject>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            if (!projectsByPath.ContainsKey(project.FullPath))
            {
                projectsByPath[project.FullPath] = project;
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
            ? symbol.TypeKind == TypeKind.Struct ? NamedTypeKind.Struct : NamedTypeKind.Record
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
                    filePath = SolutionIdentity.NormalizeRelativePath(rootPath, sourcePath);
        }

        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(symbol, compilation);
        var isExternal = !symbol.Locations.Any(location => location.IsInSource);

        var documentationId = DocumentationIdUtility.GetDocumentationId(symbol);

        return new NamedTypeDto(
            symbolKey,
            documentationId,
            name,
            ns,
            fullName,
            projectKey,
            assemblyName,
            kind,
            isExternal,
            filePath);
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

            var projectRootRepoPath = SolutionIdentity.NormalizeRelativePath(rootPath, projectRoot);
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

                var repositoryPath = SolutionIdentity.NormalizeRelativePath(rootPath, document.FilePath);
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
                var repositoryPath = SolutionIdentity.NormalizeRelativePath(rootPath, directoryFullPath);
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
        string projectName,
        string? documentationId = null,
        string? symbolKind = null,
        string? realType = null)
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
            projectName,
            documentationId,
            symbolKind,
            realType,
            null,
            null,
            null));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> BuildTreeChildrenMap(
        IReadOnlyList<CodeTreeEntryDto> entries)
    {
        var map = new Dictionary<string, List<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var parentKey = GetParentDisplayPath(entry);
            if (!map.TryGetValue(parentKey, out var children))
            {
                children = new List<CodeTreeEntryDto>();
                map[parentKey] = children;
            }

            children.Add(entry);
        }

        var finalized = new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (parent, children) in map)
        {
            var ordered = parent.Length == 0
                ? children.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                : children
                    .OrderBy(entry => GetNodeSortOrder(entry.Type))
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
            finalized[parent] = ordered.ToList();
        }

        return finalized;
    }

    private static int GetNodeSortOrder(CodeTreeEntryType type)
    {
        return type switch
        {
            CodeTreeEntryType.Directory => 0,
            CodeTreeEntryType.File => 1,
            _ => 0
        };
    }

    private static string GetParentDisplayPath(CodeTreeEntryDto entry)
    {
        if (entry.Type == CodeTreeEntryType.Project)
        {
            return string.Empty;
        }

        var displayPath = entry.DisplayPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayPath))
        {
            return string.Empty;
        }

        var lastSeparator = displayPath.LastIndexOf('/');
        return lastSeparator <= 0 ? string.Empty : displayPath[..lastSeparator];
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

    private static Guid CreateStableId(string key)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }
}
