using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Code.Analysis.Services.Complexity;
using SilkHat.Git.Analysis.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeWorkspaceLoader : ICodeWorkspaceLoader
{
    private static readonly IReadOnlyList<MetadataReference> DefaultReferences = BuildDefaultReferences();
    private readonly ILogger<CodeWorkspaceLoader> _logger;
    private readonly IGraphStoreProvider _graphStoreProvider;
    private readonly IIndexingStatusStore? _statusStore;
    private readonly IGitCommandRunner? _gitCommandRunner;
    private readonly IComplexityStrategyFactory? _complexityStrategyFactory;
    private readonly SolutionParser _solutionParser = new();
    private readonly ProjectParser _projectParser = new();
    private readonly SolutionIdentityResolver _identityResolver = new();
    private const string UnspecifiedPackageVersion = "unspecified";

    public CodeWorkspaceLoader(
        ILogger<CodeWorkspaceLoader> logger,
        IGraphStoreProvider? graphStoreProvider = null,
        IIndexingStatusStore? statusStore = null,
        IGitCommandRunner? gitCommandRunner = null,
        IComplexityStrategyFactory? complexityStrategyFactory = null)
    {
        _logger = logger;
        _graphStoreProvider = graphStoreProvider ?? new GraphStoreProvider();
        _statusStore = statusStore;
        _gitCommandRunner = gitCommandRunner;
        _complexityStrategyFactory = complexityStrategyFactory;
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

            _statusStore?.InitializeSolution(solutionId);

            var workspace = BuildWorkspace(solutionProjects);
            var workspaceProjects = workspace.CurrentSolution.Projects
                .Where(project => !string.IsNullOrWhiteSpace(project.FilePath))
                .ToList();

            var projectKeyMap = workspaceProjects.ToDictionary(p => p.Id, p => p.Id.Id.ToString("N"));
            var projectKeyToName = workspaceProjects.ToDictionary(p => projectKeyMap[p.Id], p => p.Name);
            var projectPathToKey = workspaceProjects
                .Where(p => !string.IsNullOrWhiteSpace(p.FilePath))
                .ToDictionary(
                    p => Path.GetFullPath(p.FilePath!),
                    p => projectKeyMap[p.Id],
                    StringComparer.OrdinalIgnoreCase);

            var compilations = new Dictionary<string, Compilation>();
            foreach (var project in workspaceProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _statusStore?.SetJobRunning(solutionId, IndexJobType.ProjectsAndFiles, "Building project compilations.");
                var compilation = await project.GetCompilationAsync(cancellationToken);
                if (compilation is null)
                {
                    _logger.LogWarning("Roslyn compilation was null for project {ProjectName}.", project.Name);
                    continue;
                }

                compilations[projectKeyMap[project.Id]] = compilation;
            }
            _statusStore?.SetJobCompleted(solutionId, IndexJobType.ProjectsAndFiles, "Projects loaded.");

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
            var memberNodes = new List<MemberNodeInfo>();
            var typeLocationAttributes = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<ProjectPackageReference> packageReferences = Array.Empty<ProjectPackageReference>();

            _statusStore?.SetJobRunning(solutionId, IndexJobType.TypesAndMembers, "Indexing types and members.");
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

                    var typeKey = !string.IsNullOrWhiteSpace(dto.DocumentationId)
                        ? dto.DocumentationId
                        : dto.SymbolKey;

                    var locationAttributes = BuildLocationAttributes(symbol, rootPath);
                    if (locationAttributes is not null)
                    {
                        typeLocationAttributes[typeKey] = locationAttributes;
                    }

                    memberNodes.AddRange(CollectMemberNodes(symbol, compilation, dto, projectIndex[projectKey].Name, rootPath));
                }
            }
            _statusStore?.SetJobCompleted(solutionId, IndexJobType.TypesAndMembers, "Types and members indexed.");

            try
            {
                _statusStore?.SetJobRunning(solutionId, IndexJobType.Packages, "Indexing packages.");
                packageReferences = BuildPackageReferences(solutionProjects, projectPathToKey, projectIndex);
            }
            catch (Exception ex)
            {
                _statusStore?.SetJobFailed(solutionId, IndexJobType.Packages, $"Package indexing failed: {ex.Message}");
                throw;
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

            GitLogData? gitData = null;
            if (_gitCommandRunner is null)
            {
                _statusStore?.SetJobCompleted(solutionId, IndexJobType.Git, "Git runner not configured (skipped).");
            }
            else
            {
                try
                {
                    _statusStore?.SetJobRunning(solutionId, IndexJobType.Git, "Indexing git.");
                    gitData = await BuildGitDataAsync(rootPath, cancellationToken);
                }
                catch (Exception ex)
                {
                    _statusStore?.SetJobFailed(solutionId, IndexJobType.Git, $"Git indexing failed: {ex.Message}");
                    _logger.LogWarning(ex, "Git indexing failed for solution {SolutionPath}.", parsedSolution.SolutionPath);
                }
            }

            ComplexityGraphData? complexityData = null;
            if (_complexityStrategyFactory is null)
            {
                _statusStore?.SetJobCompleted(solutionId, IndexJobType.Complexity, "Complexity strategies not configured (skipped).");
            }
            else
            {
                try
                {
                    _statusStore?.SetJobRunning(solutionId, IndexJobType.Complexity, "Computing complexity.");
                    complexityData = await BuildComplexityDataAsync(
                        rootPath,
                        solutionWorkspaces[solutionId],
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _statusStore?.SetJobFailed(solutionId, IndexJobType.Complexity, $"Complexity indexing failed: {ex.Message}");
                    _logger.LogWarning(ex, "Complexity indexing failed for solution {SolutionPath}.", parsedSolution.SolutionPath);
                }
            }

            PopulateGraph(
                solutionId,
                parsedSolution.SolutionName,
                projectIndex,
                treeEntries,
                treeChildrenMap,
                namedTypes,
                typeLocationAttributes,
                memberNodes,
                packageReferences,
                gitData,
                complexityData);

            _statusStore?.SetJobCompleted(solutionId, IndexJobType.Packages, "Packages indexed.");
            if (gitData is not null)
            {
                _statusStore?.SetJobCompleted(solutionId, IndexJobType.Git, "Git indexed.");
            }
            if (complexityData is not null)
            {
                _statusStore?.SetJobCompleted(solutionId, IndexJobType.Complexity, "Complexity indexed.");
            }
        }

        return new CodeRepositoryWorkspace(rootPath, solutionWorkspaces);
    }

    private void PopulateGraph(
        string solutionId,
        string solutionName,
        IReadOnlyDictionary<string, ProjectIndex> projects,
        IReadOnlyList<CodeTreeEntryDto> treeEntries,
        IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> treeChildrenByParent,
        IReadOnlyList<NamedTypeDto> namedTypes,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> typeLocationAttributes,
        IReadOnlyList<MemberNodeInfo> memberNodes,
        IReadOnlyList<ProjectPackageReference> packageReferences,
        GitLogData? gitData,
        ComplexityGraphData? complexityData)
    {
        var graph = _graphStoreProvider.GetOrAdd(solutionId);

        var solutionNodeId = CreateStableId($"solution:{solutionId}");
        graph.AddNode(new GraphNodeDto(solutionNodeId, GraphNodeKind.Solution, solutionId, solutionName));

        var displayPathToNodeId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var repositoryPathToNodeId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var typeNodeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var projectNodeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Values)
        {
            var projectNodeId = CreateStableId($"project:{project.ProjectKey}");
            graph.AddNode(new GraphNodeDto(projectNodeId, GraphNodeKind.Project, project.ProjectKey, project.Name));
            graph.AddEdge(solutionNodeId, projectNodeId, EdgeType.Contains);
            displayPathToNodeId[project.Name] = projectNodeId;
            projectNodeIds[project.ProjectKey] = projectNodeId;
        }

        foreach (var entry in treeEntries)
        {
            if (entry.Type == CodeTreeEntryType.Project)
            {
                // Bind project display path to the existing project node; do not create duplicates.
                if (displayPathToNodeId.TryGetValue(entry.Name, out var existingProjectId))
                {
                    displayPathToNodeId[entry.DisplayPath] = existingProjectId;
                }
                continue;
            }

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
                ["DisplayPath"] = entry.DisplayPath,
                ["ProjectKey"] = entry.ProjectKey,
                ["ProjectName"] = entry.ProjectName
            };
            if (entry.Type == CodeTreeEntryType.File)
            {
                attributes["RepositoryPath"] = entry.RepositoryPath;
            }
            else if (entry.Type == CodeTreeEntryType.Directory)
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

        var packageNodeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var packageVersionNodeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in packageReferences)
        {
            var packageKey = reference.PackageId.Trim();
            if (!packageNodeIds.TryGetValue(packageKey, out var packageNodeId))
            {
                packageNodeId = CreateStableId($"package:{packageKey}");
                var packageAttributes = new Dictionary<string, string>
                {
                    ["PackageId"] = packageKey
                };
                graph.AddNode(new GraphNodeDto(packageNodeId, GraphNodeKind.Package, packageKey, packageKey, packageAttributes));
                packageNodeIds[packageKey] = packageNodeId;
            }

            var versionKey = $"{packageKey}@{reference.Version}";
            if (!packageVersionNodeIds.TryGetValue(versionKey, out var packageVersionNodeId))
            {
                packageVersionNodeId = CreateStableId($"package-version:{versionKey}");
                var versionAttributes = new Dictionary<string, string>
                {
                    ["PackageId"] = packageKey,
                    ["Version"] = reference.Version
                };
                graph.AddNode(new GraphNodeDto(
                    packageVersionNodeId,
                    GraphNodeKind.PackageVersion,
                    versionKey,
                    reference.Version,
                    versionAttributes));
                packageVersionNodeIds[versionKey] = packageVersionNodeId;

                graph.AddEdge(packageVersionNodeId, packageNodeId, EdgeType.PackageVersion);
            }

            if (projectNodeIds.TryGetValue(reference.ProjectKey, out var projectNodeId))
            {
                graph.AddEdge(projectNodeId, packageVersionNodeId, EdgeType.DependsOnPackage);
            }
        }

        foreach (var type in namedTypes)
        {
            var typeKey = !string.IsNullOrWhiteSpace(type.DocumentationId)
                ? type.DocumentationId
                : type.SymbolKey;
            var typeNodeId = CreateStableId($"type:{typeKey}");
            var typeAttributes = new Dictionary<string, string>
            {
                ["DocumentationId"] = type.DocumentationId ?? string.Empty,
                ["SymbolKey"] = type.SymbolKey,
                ["RealType"] = type.Kind.ToString(),
                ["Name"] = type.Name,
                ["Namespace"] = type.Namespace,
                ["AssemblyName"] = type.AssemblyName,
                ["ProjectKey"] = type.ProjectKey,
                ["ProjectName"] = projects.TryGetValue(type.ProjectKey, out var projectIndex) ? projectIndex.Name : type.ProjectKey
            };
            if (!string.IsNullOrWhiteSpace(type.FilePath))
            {
                typeAttributes["RepositoryPath"] = type.FilePath;
            }

            if (typeLocationAttributes.TryGetValue(typeKey, out var locationAttrs))
            {
                foreach (var pair in locationAttrs)
                {
                    typeAttributes[pair.Key] = pair.Value;
                }
            }

            graph.AddNode(new GraphNodeDto(typeNodeId, GraphNodeKind.NamedType, typeKey, type.Name, typeAttributes));
            typeNodeIds[typeKey] = typeNodeId;

            if (!string.IsNullOrWhiteSpace(type.FilePath) &&
                repositoryPathToNodeId.TryGetValue(type.FilePath, out var fileNodeId))
            {
                graph.AddEdge(fileNodeId, typeNodeId, EdgeType.DeclaresType);
            }
        }

        foreach (var member in memberNodes)
        {
            if (!typeNodeIds.TryGetValue(member.ParentKey, out var parentTypeId))
            {
                continue;
            }

            var memberNodeId = CreateStableId($"member:{member.Key}");
            var attributes = new Dictionary<string, string>(member.Attributes)
            {
                ["ProjectKey"] = member.ProjectKey,
                ["ProjectName"] = member.ProjectName
            };

            if (!string.IsNullOrWhiteSpace(member.FilePath))
            {
                attributes["RepositoryPath"] = member.FilePath!;
            }

            graph.AddNode(new GraphNodeDto(memberNodeId, member.Kind, member.Key, member.Name, attributes));
            graph.AddEdge(parentTypeId, memberNodeId, EdgeType.DeclaresMember);

            if (!string.IsNullOrWhiteSpace(member.ReturnOrValueTypeDocumentationId)
                && typeNodeIds.TryGetValue(member.ReturnOrValueTypeDocumentationId, out var returnTypeId))
            {
                var edgeType = member.Kind switch
                {
                    GraphNodeKind.Property => EdgeType.PropertyType,
                    GraphNodeKind.Field => EdgeType.FieldType,
                    _ => EdgeType.ReturnType
                };
                graph.AddEdge(memberNodeId, returnTypeId, edgeType);
            }

            foreach (var parameter in member.Parameters)
            {
                var parameterNodeId = CreateStableId($"parameter:{parameter.Key}");
                var parameterAttributes = new Dictionary<string, string>(parameter.Attributes)
                {
                    ["ProjectKey"] = member.ProjectKey,
                    ["ProjectName"] = member.ProjectName
                };

                graph.AddNode(new GraphNodeDto(parameterNodeId, GraphNodeKind.Parameter, parameter.Key, parameter.Name, parameterAttributes));
                graph.AddEdge(memberNodeId, parameterNodeId, EdgeType.DeclaresParameter);

                if (!string.IsNullOrWhiteSpace(parameter.TypeDocumentationId)
                    && typeNodeIds.TryGetValue(parameter.TypeDocumentationId, out var parameterTypeId))
            {
                graph.AddEdge(parameterNodeId, parameterTypeId, EdgeType.ParameterType);
            }
        }

        if (gitData is not null)
        {
            var authorNodeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var commit in gitData.Commits)
            {
                var commitNodeId = CreateStableId($"git-commit:{commit.Sha}");
                var commitAttributes = new Dictionary<string, string>
                {
                    ["Sha"] = commit.Sha,
                    ["AuthorName"] = commit.AuthorName,
                    ["AuthorEmail"] = commit.AuthorEmail ?? string.Empty
                };
                if (commit.Date.HasValue)
                {
                    commitAttributes["Date"] = commit.Date.Value.ToString("O");
                }

                graph.AddNode(new GraphNodeDto(commitNodeId, GraphNodeKind.GitCommit, commit.Sha, commit.Sha, commitAttributes));
                graph.AddEdge(solutionNodeId, commitNodeId, EdgeType.Contains);

                var authorKey = string.IsNullOrWhiteSpace(commit.AuthorEmail) ? commit.AuthorName : commit.AuthorEmail!;
                if (!authorNodeIds.TryGetValue(authorKey, out var authorNodeId))
                {
                    authorNodeId = CreateStableId($"git-author:{authorKey}");
                    var authorAttributes = new Dictionary<string, string>
                    {
                        ["Name"] = commit.AuthorName,
                        ["Email"] = commit.AuthorEmail ?? string.Empty
                    };
                    graph.AddNode(new GraphNodeDto(authorNodeId, GraphNodeKind.GitAuthor, authorKey, commit.AuthorName, authorAttributes));
                    authorNodeIds[authorKey] = authorNodeId;
                }

                graph.AddEdge(commitNodeId, authorNodeId, EdgeType.AuthoredBy);

                foreach (var change in commit.Changes)
                {
                    var normalizedPath = NormalizePathKey(change.Path);
                    if (!repositoryPathToNodeId.TryGetValue(normalizedPath, out var fileNodeId))
                    {
                        continue;
                    }

                    var changeAttributes = new Dictionary<string, string>
                    {
                        ["Added"] = change.Added.ToString(CultureInfo.InvariantCulture),
                        ["Deleted"] = change.Deleted.ToString(CultureInfo.InvariantCulture),
                        ["RepositoryPath"] = normalizedPath
                    };

                    graph.AddEdge(commitNodeId, fileNodeId, EdgeType.Changes, changeAttributes);
                }
            }
        }

        if (complexityData is not null)
        {
            foreach (var metric in complexityData.MethodMetrics)
            {
                var metricNodeId = CreateStableId($"complexity:method:{metric.DocId}:{metric.Measure}");
                var metricAttributes = new Dictionary<string, string>
                {
                    ["DocId"] = metric.DocId,
                    ["Measure"] = metric.Measure.ToString(),
                    ["Value"] = metric.Value.ToString(CultureInfo.InvariantCulture),
                    ["TargetKind"] = "Method"
                };
                graph.AddNode(new GraphNodeDto(metricNodeId, GraphNodeKind.ComplexityMetric, $"{metric.DocId}:{metric.Measure}", metric.Measure.ToString(), metricAttributes));

                if (typeNodeIds.TryGetValue(metric.DocId, out var methodNodeId))
                {
                    graph.AddEdge(methodNodeId, metricNodeId, EdgeType.HasMetric);
                }
            }

            foreach (var metric in complexityData.TypeMetrics)
            {
                var metricNodeId = CreateStableId($"complexity:type:{metric.DocId}:{metric.Measure}");
                var metricAttributes = new Dictionary<string, string>
                {
                    ["DocId"] = metric.DocId,
                    ["Measure"] = metric.Measure.ToString(),
                    ["Value"] = metric.Value.ToString(CultureInfo.InvariantCulture),
                    ["TargetKind"] = "Type"
                };
                graph.AddNode(new GraphNodeDto(metricNodeId, GraphNodeKind.ComplexityMetric, $"{metric.DocId}:{metric.Measure}", metric.Measure.ToString(), metricAttributes));

                if (typeNodeIds.TryGetValue(metric.DocId, out var typeNodeId))
                {
                    graph.AddEdge(typeNodeId, metricNodeId, EdgeType.HasMetric);
                }
            }
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

    private static IReadOnlyList<ProjectPackageReference> BuildPackageReferences(
        IReadOnlyDictionary<string, ParsedProject> parsedProjects,
        IReadOnlyDictionary<string, string> projectPathToKey,
        IReadOnlyDictionary<string, ProjectIndex> projectIndex)
    {
        var references = new List<ProjectPackageReference>();

        foreach (var (projectPath, parsedProject) in parsedProjects)
        {
            if (!projectPathToKey.TryGetValue(projectPath, out var projectKey))
            {
                continue;
            }

            var projectName = projectIndex.TryGetValue(projectKey, out var index)
                ? index.Name
                : projectKey;

            foreach (var package in parsedProject.PackageReferences)
            {
                var version = string.IsNullOrWhiteSpace(package.Version)
                    ? UnspecifiedPackageVersion
                    : package.Version!;

                references.Add(new ProjectPackageReference(
                    projectKey,
                    projectName,
                    package.Id.Trim(),
                    version.Trim()));
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

    private static IReadOnlyDictionary<string, string>? BuildLocationAttributes(ISymbol symbol, string rootPath)
    {
        var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef?.SyntaxTree?.FilePath is null)
        {
            var location = symbol.Locations.FirstOrDefault(loc => loc.IsInSource);
            if (location?.SourceTree?.FilePath is null)
            {
                return null;
            }

            var spanFallback = location.SourceSpan;
            var lineSpanFallback = location.GetLineSpan();
            var normalizedPathFallback = SolutionIdentity.NormalizeRelativePath(rootPath, location.SourceTree.FilePath);

            return new Dictionary<string, string>
            {
                ["FilePath"] = normalizedPathFallback,
                ["SpanStart"] = spanFallback.Start.ToString(),
                ["SpanLength"] = spanFallback.Length.ToString(),
                ["StartLine"] = (lineSpanFallback.StartLinePosition.Line + 1).ToString(),
                ["StartColumn"] = (lineSpanFallback.StartLinePosition.Character + 1).ToString(),
                ["EndLine"] = (lineSpanFallback.EndLinePosition.Line + 1).ToString(),
                ["EndColumn"] = (lineSpanFallback.EndLinePosition.Character + 1).ToString()
            };
        }

        var syntax = syntaxRef.GetSyntax();
        var tree = syntax.SyntaxTree;
        var span = syntax.FullSpan; // include leading trivia to highlight full definition
        var lineSpan = tree.GetLineSpan(span);
        var normalizedPath = SolutionIdentity.NormalizeRelativePath(rootPath, tree.FilePath);

        return new Dictionary<string, string>
        {
            ["FilePath"] = normalizedPath,
            ["SpanStart"] = span.Start.ToString(),
            ["SpanLength"] = span.Length.ToString(),
            ["StartLine"] = (lineSpan.StartLinePosition.Line + 1).ToString(),
            ["StartColumn"] = (lineSpan.StartLinePosition.Character + 1).ToString(),
            ["EndLine"] = (lineSpan.EndLinePosition.Line + 1).ToString(),
            ["EndColumn"] = (lineSpan.EndLinePosition.Character + 1).ToString()
        };
    }

    private static IReadOnlyList<MemberNodeInfo> CollectMemberNodes(
        INamedTypeSymbol type,
        Compilation compilation,
        NamedTypeDto typeDto,
        string projectName,
        string rootPath)
    {
        var results = new List<MemberNodeInfo>();
        var parentKey = !string.IsNullOrWhiteSpace(typeDto.DocumentationId) ? typeDto.DocumentationId : typeDto.SymbolKey;

        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol field:
                    {
                        var docId = DocumentationIdUtility.GetDocumentationId(field);
                        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(field, compilation);
                        var key = !string.IsNullOrWhiteSpace(docId) ? docId : symbolKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        var memberAttributes = new Dictionary<string, string>
                        {
                            ["DocumentationId"] = docId ?? string.Empty,
                            ["SymbolKey"] = symbolKey,
                            ["RealType"] = "Field",
                            ["Name"] = field.Name
                        };

                        var locationField = BuildLocationAttributes(field, rootPath);
                        if (locationField is not null)
                        {
                            foreach (var pair in locationField)
                            {
                                memberAttributes[pair.Key] = pair.Value;
                            }
                        }

                        results.Add(new MemberNodeInfo(
                            key,
                            field.Name,
                            GraphNodeKind.Field,
                            parentKey,
                            typeDto.ProjectKey,
                            projectName,
                            memberAttributes.TryGetValue("FilePath", out var fp) ? fp : typeDto.FilePath,
                            DocumentationIdUtility.GetDocumentationId(field.Type),
                            new List<ParameterNodeInfo>(),
                            memberAttributes));
                        break;
                    }
                case IPropertySymbol property:
                    {
                        var docId = DocumentationIdUtility.GetDocumentationId(property);
                        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(property, compilation);
                        var key = !string.IsNullOrWhiteSpace(docId) ? docId : symbolKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        var memberAttributes = new Dictionary<string, string>
                        {
                            ["DocumentationId"] = docId ?? string.Empty,
                            ["SymbolKey"] = symbolKey,
                            ["RealType"] = "Property",
                            ["Name"] = property.Name
                        };

                        var locationProperty = BuildLocationAttributes(property, rootPath);
                        if (locationProperty is not null)
                        {
                            foreach (var pair in locationProperty)
                            {
                                memberAttributes[pair.Key] = pair.Value;
                            }
                        }

                        results.Add(new MemberNodeInfo(
                            key,
                            property.Name,
                            GraphNodeKind.Property,
                            parentKey,
                            typeDto.ProjectKey,
                            projectName,
                            memberAttributes.TryGetValue("FilePath", out var fp) ? fp : typeDto.FilePath,
                            DocumentationIdUtility.GetDocumentationId(property.Type),
                            new List<ParameterNodeInfo>(),
                            memberAttributes));
                        break;
                    }
                case IEventSymbol @event:
                    {
                        var docId = DocumentationIdUtility.GetDocumentationId(@event);
                        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(@event, compilation);
                        var key = !string.IsNullOrWhiteSpace(docId) ? docId : symbolKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        var memberAttributes = new Dictionary<string, string>
                        {
                            ["DocumentationId"] = docId ?? string.Empty,
                            ["SymbolKey"] = symbolKey,
                            ["RealType"] = "Event",
                            ["Name"] = @event.Name
                        };

                        var locationEvent = BuildLocationAttributes(@event, rootPath);
                        if (locationEvent is not null)
                        {
                            foreach (var pair in locationEvent)
                            {
                                memberAttributes[pair.Key] = pair.Value;
                            }
                        }

                        results.Add(new MemberNodeInfo(
                            key,
                            @event.Name,
                            GraphNodeKind.Field,
                            parentKey,
                            typeDto.ProjectKey,
                            projectName,
                            memberAttributes.TryGetValue("FilePath", out var fp) ? fp : typeDto.FilePath,
                            DocumentationIdUtility.GetDocumentationId(@event.Type),
                            new List<ParameterNodeInfo>(),
                            memberAttributes));
                        break;
                    }
                case IMethodSymbol method:
                    {
                        if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove)
                        {
                            continue;
                        }

                        var docId = DocumentationIdUtility.GetDocumentationId(method);
                        var symbolKey = SymbolKeyUtility.GetSymbolKeyString(method, compilation);
                        var key = !string.IsNullOrWhiteSpace(docId) ? docId : symbolKey;
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        var realType = method.MethodKind switch
                        {
                            MethodKind.Constructor => "Constructor",
                            MethodKind.StaticConstructor => "StaticConstructor",
                            _ => "Method"
                        };

                        var memberAttributes = new Dictionary<string, string>
                        {
                            ["DocumentationId"] = docId ?? string.Empty,
                            ["SymbolKey"] = symbolKey,
                            ["RealType"] = realType,
                            ["Name"] = method.Name
                        };

                        var location = BuildLocationAttributes(method, rootPath);
                        if (location is not null)
                        {
                            foreach (var pair in location)
                            {
                                memberAttributes[pair.Key] = pair.Value;
                            }
                        }

                        var parameters = new List<ParameterNodeInfo>();
                        for (var i = 0; i < method.Parameters.Length; i++)
                        {
                            var parameter = method.Parameters[i];
                            var parameterKey = $"{key}:param:{i}:{parameter.Name}";
                            var parameterAttributes = new Dictionary<string, string>
                            {
                                ["RealType"] = "Parameter",
                                ["Name"] = parameter.Name,
                                ["Ordinal"] = i.ToString()
                            };
                            var parameterLocation = BuildLocationAttributes(parameter, rootPath);
                            if (parameterLocation is not null)
                            {
                                foreach (var pair in parameterLocation)
                                {
                                    parameterAttributes[pair.Key] = pair.Value;
                                }
                            }

                            parameters.Add(new ParameterNodeInfo(
                                parameterKey,
                                parameter.Name,
                                i,
                                DocumentationIdUtility.GetDocumentationId(parameter.Type),
                                parameterAttributes));
                        }

                        results.Add(new MemberNodeInfo(
                            key,
                            method.Name,
                            GraphNodeKind.Method,
                            parentKey,
                            typeDto.ProjectKey,
                            projectName,
                            memberAttributes.TryGetValue("FilePath", out var fp) ? fp : typeDto.FilePath,
                            DocumentationIdUtility.GetDocumentationId(method.ReturnType),
                            parameters,
                            memberAttributes));
                        break;
                    }
            }
        }

        return results;
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

    private sealed record ParameterNodeInfo(
        string Key,
        string Name,
        int Ordinal,
        string? TypeDocumentationId,
        IReadOnlyDictionary<string, string> Attributes);

    private sealed record MemberNodeInfo(
        string Key,
        string Name,
        GraphNodeKind Kind,
        string ParentKey,
        string ProjectKey,
        string ProjectName,
        string? FilePath,
        string? ReturnOrValueTypeDocumentationId,
        IReadOnlyList<ParameterNodeInfo> Parameters,
        IReadOnlyDictionary<string, string> Attributes);

    private sealed record ProjectPackageReference(
        string ProjectKey,
        string ProjectName,
        string PackageId,
        string Version);

    private sealed record GitChangeData(string Path, int Added, int Deleted);

    private sealed record GitCommitData(
        string Sha,
        string AuthorName,
        string? AuthorEmail,
        DateTimeOffset? Date,
        IReadOnlyList<GitChangeData> Changes);

    private sealed record GitLogData(
        IReadOnlyList<GitCommitData> Commits);

    private sealed record ComplexityMetricData(
        string DocId,
        ComplexityMeasureType Measure,
        int Value);

    private sealed record ComplexityGraphData(
        IReadOnlyList<ComplexityMetricData> MethodMetrics,
        IReadOnlyList<ComplexityMetricData> TypeMetrics);

    private static Guid CreateStableId(string key)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private async Task<GitLogData?> BuildGitDataAsync(string repoRoot, CancellationToken cancellationToken)
    {
        if (_gitCommandRunner is null)
        {
            return null;
        }

        var result = await _gitCommandRunner.ExecuteAsync(
            repoRoot,
            new[] { "log", "--numstat", "--pretty=format:COMMIT|%H|%an|%ae|%ad", "--" },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "Git log failed while indexing."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        var commits = new List<GitCommitData>();
        string? currentSha = null;
        string? currentAuthor = null;
        string? currentEmail = null;
        DateTimeOffset? currentDate = null;
        var currentChanges = new List<GitChangeData>();

        var lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("COMMIT|", StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(currentSha))
                {
                    commits.Add(new GitCommitData(
                        currentSha!,
                        currentAuthor ?? string.Empty,
                        currentEmail,
                        currentDate,
                        currentChanges.ToList()));
                }

                currentChanges.Clear();
                var parts = line.Split('|');
                currentSha = parts.ElementAtOrDefault(1)?.Trim();
                currentAuthor = parts.ElementAtOrDefault(2)?.Trim();
                currentEmail = parts.ElementAtOrDefault(3)?.Trim();
                currentDate = null;
                if (DateTimeOffset.TryParse(parts.ElementAtOrDefault(4), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate))
                {
                    currentDate = parsedDate;
                }

                continue;
            }

            var segments = line.Split('\t');
            if (segments.Length < 3)
            {
                continue;
            }

            var path = NormalizePathKey(ResolveRenamePath(segments[2].Trim()));
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var added = segments[0] == "-" ? 0 : int.TryParse(segments[0], out var a) ? a : 0;
            var deleted = segments[1] == "-" ? 0 : int.TryParse(segments[1], out var d) ? d : 0;
            currentChanges.Add(new GitChangeData(path, added, deleted));
        }

        if (!string.IsNullOrWhiteSpace(currentSha))
        {
            commits.Add(new GitCommitData(
                currentSha!,
                currentAuthor ?? string.Empty,
                currentEmail,
                currentDate,
                currentChanges.ToList()));
        }

        return new GitLogData(commits);
    }

    private static string ResolveRenamePath(string path)
    {
        if (!path.Contains("=>", StringComparison.Ordinal))
        {
            return path;
        }

        if (path.Contains('{') && path.Contains('}'))
        {
            var open = path.IndexOf('{');
            var close = path.IndexOf('}');
            if (open >= 0 && close > open)
            {
                var prefix = path[..open];
                var suffix = path[(close + 1)..];
                var segment = path[(open + 1)..close];
                var parts = segment.Split("=>", 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var newSegment = parts[1].Trim();
                    return $"{prefix}{newSegment}{suffix}";
                }
            }
        }

        var arrowIndex = path.LastIndexOf("=>", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            return path[(arrowIndex + 2)..].Trim();
        }

        return path;
    }

    private async Task<ComplexityGraphData?> BuildComplexityDataAsync(
        string rootPath,
        CodeSolutionWorkspace solution,
        CancellationToken cancellationToken)
    {
        if (_complexityStrategyFactory is null)
        {
            return null;
        }

        var methodMetrics = new ConcurrentBag<ComplexityMetricData>();
        var typeMetrics = new ConcurrentBag<ComplexityMetricData>();

        var strategies = new Dictionary<ComplexityMeasureType, IComplexityStrategy>
        {
            [ComplexityMeasureType.Cognitive] = _complexityStrategyFactory.GetStrategy(ComplexityMeasureType.Cognitive),
            [ComplexityMeasureType.Cyclomatic] = _complexityStrategyFactory.GetStrategy(ComplexityMeasureType.Cyclomatic),
            [ComplexityMeasureType.Indentation] = _complexityStrategyFactory.GetStrategy(ComplexityMeasureType.Indentation)
        };

        var treeToCompilation = new Dictionary<SyntaxTree, Compilation>();
        foreach (var compilation in solution.Compilations.Values)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                if (!treeToCompilation.ContainsKey(tree))
                {
                    treeToCompilation[tree] = compilation;
                }
            }
        }

        var syntaxTrees = treeToCompilation.Keys
            .Where(tree => !string.IsNullOrWhiteSpace(tree.FilePath))
            .ToList();

        await Parallel.ForEachAsync(
            syntaxTrees,
            cancellationToken,
            async (tree, ct) =>
            {
                if (!treeToCompilation.TryGetValue(tree, out var compilation))
                {
                    return;
                }

                var semanticModel = compilation.GetSemanticModel(tree);
                var sourceText = await tree.GetTextAsync(ct);
                var methodNodes = tree.GetRoot(ct)
                    .DescendantNodes()
                    .OfType<BaseMethodDeclarationSyntax>()
                    .ToList();

                foreach (var method in methodNodes)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(method, ct);
                    var typeDocId = methodSymbol?.ContainingType is not null
                        ? DocumentationIdUtility.GetDocumentationId(methodSymbol.ContainingType)
                        : null;
                    var methodDocId = methodSymbol is not null
                        ? DocumentationIdUtility.GetDocumentationId(methodSymbol)
                        : null;

                    foreach (var (measure, strategy) in strategies)
                    {
                        var value = strategy.Compute(method, semanticModel, sourceText);
                        if (!string.IsNullOrWhiteSpace(methodDocId))
                        {
                            methodMetrics.Add(new ComplexityMetricData(methodDocId!, measure, value));
                        }

                        if (!string.IsNullOrWhiteSpace(typeDocId))
                        {
                            typeMetrics.Add(new ComplexityMetricData(typeDocId!, measure, value));
                        }
                    }
                }
            });

        return new ComplexityGraphData(methodMetrics.ToList(), typeMetrics.ToList());
    }
}
