using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;
using SilkHat.Git.Analysis.Models;

namespace SilkHat.Api.Services
{
    public sealed record CodeTreeMetricValues(
        CodeTreeAnnotationKind Kind,
        IReadOnlyDictionary<string, int> FileValues,
        IReadOnlyDictionary<string, int> NodeValues,
        IReadOnlyDictionary<string, int> TypeValuesByDocId,
        IReadOnlyDictionary<string, int> MemberValuesByDocId);

    public interface ICodeTreeMetricsService
    {
        Task<CodeTreeMetricValues> GetMetricsAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CodeTreeAnnotationKind kind,
            CancellationToken cancellationToken);
    }

    public sealed class CodeTreeMetricsService : ICodeTreeMetricsService
    {
        private readonly ICodeTreeMetricsCacheStore _cacheStore;
        private readonly IComplexityMetricsAggregator _complexityAggregator;
        private readonly IGitMetricsAggregator _gitMetricsAggregator;
        private readonly IGraphQueryService _graphQueryService;

        public CodeTreeMetricsService(
            ICodeTreeMetricsCacheStore cacheStore,
            IComplexityMetricsAggregator complexityAggregator,
            IGitMetricsAggregator gitMetricsAggregator,
            IGraphQueryService graphQueryService)
        {
            _cacheStore = cacheStore;
            _complexityAggregator = complexityAggregator;
            _gitMetricsAggregator = gitMetricsAggregator;
            _graphQueryService = graphQueryService;
        }

        public Task<CodeTreeMetricValues> GetMetricsAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CodeTreeAnnotationKind kind,
            CancellationToken cancellationToken)
        {
            if (workspace is null) throw new ArgumentNullException(nameof(workspace));

            if (solution is null) throw new ArgumentNullException(nameof(solution));

            if (_cacheStore.TryGet(configId, solution.SolutionId, kind, out var cached) && cached is not null)
                return Task.FromResult(cached);

            return BuildAndStoreMetricsAsync(configId, workspace, solution, kind, cancellationToken);
        }

        private async Task<CodeTreeMetricValues> BuildAndStoreMetricsAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CodeTreeAnnotationKind kind,
            CancellationToken cancellationToken)
        {
            var metrics = await BuildMetricsAsync(configId, workspace, solution, kind, cancellationToken);
            _cacheStore.Set(configId, solution.SolutionId, kind, metrics);
            return metrics;
        }

        private async Task<CodeTreeMetricValues> BuildMetricsAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CodeTreeAnnotationKind kind,
            CancellationToken cancellationToken)
        {
            var snapshot = _graphQueryService.GetSnapshot(solution.SolutionId);
            if (snapshot.Nodes.Count == 0)
                return await BuildMetricsFromTreeEntriesAsync(configId, workspace, solution, kind, cancellationToken);

            var nodesById = snapshot.Nodes.ToDictionary(n => n.Id);
            var parentsByChild = BuildParentMap(snapshot.Edges);

            var fileValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var nodeValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, HashSet<string>>? nodeAuthorSets = null;
            var typeValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var memberValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in snapshot.Nodes.Where(n =>
                         n.Kind is GraphNodeKind.Solution or GraphNodeKind.Project or GraphNodeKind.Folder
                             or GraphNodeKind.File))
            {
                var display = node.Attributes?.GetValueOrDefault("DisplayPath") ?? node.Key;
                nodeValues.TryAdd(display, 0);
            }

            var gitMetrics = kind is CodeTreeAnnotationKind.FileAuthorCount or CodeTreeAnnotationKind.FileChangeCount
                ? await _gitMetricsAggregator.GetMetricsAsync(configId, workspace.RootPath, cancellationToken)
                : null;

            var complexityMetrics =
                kind is CodeTreeAnnotationKind.FileAuthorCount or CodeTreeAnnotationKind.FileChangeCount
                    ? null
                    : await _complexityAggregator.GetMetricsAsync(configId, workspace, solution, cancellationToken);

            if (kind == CodeTreeAnnotationKind.FileAuthorCount)
                nodeAuthorSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var fileNode in snapshot.Nodes.Where(n => n.Kind == GraphNodeKind.File))
            {
                var displayPath = fileNode.Attributes?.GetValueOrDefault("DisplayPath") ?? fileNode.Key;
                var repoPath = fileNode.Attributes?.GetValueOrDefault("RepositoryPath") ?? displayPath;

                var value = GetFileMetric(repoPath, displayPath, kind, gitMetrics, complexityMetrics);
                fileValues[displayPath] = value;

                if (kind == CodeTreeAnnotationKind.FileAuthorCount)
                {
                    var authors = GetFileAuthors(repoPath, gitMetrics);
                    AddAuthorsToNodeValues(nodeAuthorSets!, parentsByChild, nodesById, fileNode.Id, authors);
                }
                else
                {
                    AddMetricToNodeValues(nodeValues, parentsByChild, nodesById, fileNode.Id, value);
                }
            }

            if (nodeAuthorSets is not null)
                foreach (var (key, authors) in nodeAuthorSets)
                    nodeValues[key] = authors.Count;

            if (complexityMetrics is not null)
            {
                var measure = MapComplexityMeasure(kind);

                foreach (var kvp in complexityMetrics.GetTypes(measure)) typeValues[kvp.Key] = kvp.Value;

                foreach (var kvp in complexityMetrics.GetMethods(measure)) memberValues[kvp.Key] = kvp.Value;
            }

            return new CodeTreeMetricValues(kind, fileValues, nodeValues, typeValues, memberValues);
        }

        private async Task<CodeTreeMetricValues> BuildMetricsFromTreeEntriesAsync(
            Guid configId,
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            CodeTreeAnnotationKind kind,
            CancellationToken cancellationToken)
        {
            var fileValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var nodeValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, HashSet<string>>? nodeAuthorSets = null;
            var typeValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var memberValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in solution.TreeEntries.Where(entry =>
                         entry.Type is CodeTreeEntryType.Project or CodeTreeEntryType.Directory
                             or CodeTreeEntryType.File))
                if (!nodeValues.ContainsKey(entry.DisplayPath))
                    nodeValues[entry.DisplayPath] = 0;

            var fileEntries = solution.TreeEntries
                .Where(entry => entry.Type == CodeTreeEntryType.File)
                .ToList();

            var gitMetrics = kind is CodeTreeAnnotationKind.FileAuthorCount or CodeTreeAnnotationKind.FileChangeCount
                ? await _gitMetricsAggregator.GetMetricsAsync(configId, workspace.RootPath, cancellationToken)
                : null;

            var complexityMetrics =
                kind is CodeTreeAnnotationKind.FileAuthorCount or CodeTreeAnnotationKind.FileChangeCount
                    ? null
                    : await _complexityAggregator.GetMetricsAsync(configId, workspace, solution, cancellationToken);

            if (kind == CodeTreeAnnotationKind.FileAuthorCount)
                nodeAuthorSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in fileEntries)
            {
                var value = GetFileMetric(entry.RepositoryPath, entry.DisplayPath, kind, gitMetrics, complexityMetrics);
                fileValues[entry.DisplayPath] = value;

                if (kind == CodeTreeAnnotationKind.FileAuthorCount)
                {
                    var authors = GetFileAuthors(entry.RepositoryPath, gitMetrics);
                    AddAuthorsToNodeValues(nodeAuthorSets!, entry.DisplayPath, authors);
                }
                else
                {
                    AddMetricToNodeValues(nodeValues, entry.DisplayPath, value);
                }
            }

            if (nodeAuthorSets is not null)
                foreach (var (key, authors) in nodeAuthorSets)
                    nodeValues[key] = authors.Count;

            if (complexityMetrics is not null)
            {
                var measure = MapComplexityMeasure(kind);

                foreach (var kvp in complexityMetrics.GetTypes(measure)) typeValues[kvp.Key] = kvp.Value;

                foreach (var kvp in complexityMetrics.GetMethods(measure)) memberValues[kvp.Key] = kvp.Value;
            }

            return new CodeTreeMetricValues(kind, fileValues, nodeValues, typeValues, memberValues);
        }

        private static ComplexityMeasureType MapComplexityMeasure(CodeTreeAnnotationKind kind)
        {
            return kind switch
            {
                CodeTreeAnnotationKind.CognitiveComplexity => ComplexityMeasureType.Cognitive,
                CodeTreeAnnotationKind.CyclomaticComplexity => ComplexityMeasureType.Cyclomatic,
                CodeTreeAnnotationKind.IndentationComplexity => ComplexityMeasureType.Indentation,
                _ => ComplexityMeasureType.Cognitive
            };
        }

        private static int GetFileMetric(
            string repositoryPath,
            string displayPath,
            CodeTreeAnnotationKind kind,
            GitFileMetricsSummary? gitMetrics,
            FileComplexityMetrics? complexityMetrics)
        {
            return kind switch
            {
                CodeTreeAnnotationKind.FileAuthorCount => gitMetrics?.AuthorCounts.TryGetValue(repositoryPath,
                    out var authors) == true
                    ? authors
                    : 0,
                CodeTreeAnnotationKind.FileChangeCount => gitMetrics?.ChangeCounts.TryGetValue(repositoryPath,
                    out var changes) == true
                    ? changes
                    : 0,
                _ => complexityMetrics?.GetValues(MapComplexityMeasure(kind))
                    .TryGetValue(repositoryPath, out var value) == true
                    ? value
                    : 0
            };
        }

        private static IReadOnlyCollection<string> GetFileAuthors(string repositoryPath,
            GitFileMetricsSummary? gitMetrics)
        {
            if (gitMetrics is null) return Array.Empty<string>();

            return gitMetrics.AuthorsByFile.TryGetValue(repositoryPath, out var authors)
                ? authors
                : Array.Empty<string>();
        }

        private static void AddMetricToNodeValues(
            IDictionary<string, int> nodeValues,
            string displayPath,
            int value)
        {
            if (nodeValues.TryGetValue(displayPath, out var current))
                nodeValues[displayPath] = current + value;
            else
                nodeValues[displayPath] = value;

            var parent = GetParentDisplayPath(displayPath);
            while (!string.IsNullOrEmpty(parent))
            {
                if (nodeValues.TryGetValue(parent, out var existing))
                    nodeValues[parent] = existing + value;
                else
                    nodeValues[parent] = value;

                parent = GetParentDisplayPath(parent);
            }
        }

        private static void AddAuthorsToNodeValues(
            IDictionary<string, HashSet<string>> nodeAuthorSets,
            string displayPath,
            IReadOnlyCollection<string> authors)
        {
            if (!nodeAuthorSets.TryGetValue(displayPath, out var current))
            {
                current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                nodeAuthorSets[displayPath] = current;
            }

            current.UnionWith(authors);

            var parent = GetParentDisplayPath(displayPath);
            while (!string.IsNullOrEmpty(parent))
            {
                if (!nodeAuthorSets.TryGetValue(parent, out var existing))
                {
                    existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    nodeAuthorSets[parent] = existing;
                }

                existing.UnionWith(authors);

                parent = GetParentDisplayPath(parent);
            }
        }

        private static Dictionary<Guid, List<Guid>> BuildParentMap(IEnumerable<GraphEdgeDto> edges)
        {
            var map = new Dictionary<Guid, List<Guid>>();
            foreach (var edge in edges.Where(e => e.EdgeType == EdgeType.Contains))
            {
                if (!map.TryGetValue(edge.TargetId, out var list))
                {
                    list = new List<Guid>();
                    map[edge.TargetId] = list;
                }

                list.Add(edge.SourceId);
            }

            return map;
        }

        private static void AddMetricToNodeValues(
            IDictionary<string, int> nodeValues,
            IReadOnlyDictionary<Guid, List<Guid>> parentsByChild,
            IReadOnlyDictionary<Guid, GraphNodeDto> nodesById,
            Guid nodeId,
            int value)
        {
            var queue = new Queue<Guid>();
            queue.Enqueue(nodeId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var key = nodesById.TryGetValue(current, out var node)
                    ? node.Attributes?.GetValueOrDefault("DisplayPath") ?? node.Key
                    : null;

                if (!string.IsNullOrWhiteSpace(key))
                    nodeValues[key] = nodeValues.TryGetValue(key, out var existing) ? existing + value : value;

                if (parentsByChild.TryGetValue(current, out var parents))
                    foreach (var parent in parents)
                        queue.Enqueue(parent);
            }
        }

        private static void AddAuthorsToNodeValues(
            IDictionary<string, HashSet<string>> nodeAuthorSets,
            IReadOnlyDictionary<Guid, List<Guid>> parentsByChild,
            IReadOnlyDictionary<Guid, GraphNodeDto> nodesById,
            Guid nodeId,
            IReadOnlyCollection<string> authors)
        {
            var queue = new Queue<Guid>();
            queue.Enqueue(nodeId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var key = nodesById.TryGetValue(current, out var node)
                    ? node.Attributes?.GetValueOrDefault("DisplayPath") ?? node.Key
                    : null;

                if (!string.IsNullOrWhiteSpace(key))
                {
                    if (!nodeAuthorSets.TryGetValue(key, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        nodeAuthorSets[key] = set;
                    }

                    set.UnionWith(authors);
                }

                if (parentsByChild.TryGetValue(current, out var parents))
                    foreach (var parent in parents)
                        queue.Enqueue(parent);
            }
        }

        private static string GetParentDisplayPath(string displayPath)
        {
            if (string.IsNullOrWhiteSpace(displayPath)) return string.Empty;

            var lastSeparator = displayPath.LastIndexOf('/');
            return lastSeparator <= 0 ? string.Empty : displayPath[..lastSeparator];
        }
    }
}