using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Services;

public interface ICodeTreeQueryService
{
    Task<PagedResult<CodeTreeEntryDto>> GetTreeAsync(
        Guid configId,
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        CodeTreeQuery query,
        CancellationToken cancellationToken);
}

public sealed class CodeTreeQueryService : ICodeTreeQueryService
{
    private readonly ICodeTreeService _treeService;
    private readonly ICodeTreeMetricsService _metricsService;
    private readonly IGraphQueryService _graphQueryService;

    public CodeTreeQueryService(
        ICodeTreeService treeService,
        ICodeTreeMetricsService metricsService,
        IGraphQueryService graphQueryService)
    {
        _treeService = treeService;
        _metricsService = metricsService;
        _graphQueryService = graphQueryService;
    }

    public async Task<PagedResult<CodeTreeEntryDto>> GetTreeAsync(
        Guid configId,
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        CodeTreeQuery query,
        CancellationToken cancellationToken)
    {
        if (workspace is null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var paging = new PagingQuery { PageNumber = query.PageNumber, PageSize = query.PageSize }.ResolvePaging();
        var children = (await _treeService.GetTreeAsync(workspace, solution, query.ParentId, cancellationToken)).ToList();

        HashSet<string>? includedNodes = null;
        CodeTreeMetricValues? filterMetrics = null;
        if (HasFilter(query))
        {
            filterMetrics = await _metricsService.GetMetricsAsync(
                configId,
                workspace,
                solution,
                query.FilterMetric!.Value,
                cancellationToken);

            var snapshot = _graphQueryService.GetSnapshot(solution.SolutionId);
            includedNodes = BuildIncludedSet(
                snapshot,
                filterMetrics.FileValues,
                query.FilterOperator!.Value,
                query.FilterThreshold!.Value);

            if (query.ParentId is not null
                && !string.IsNullOrWhiteSpace(query.ParentId)
                && !includedNodes.Contains(query.ParentId))
            {
                return Array.Empty<CodeTreeEntryDto>().ToPagedResult(paging);
            }
        }

        if (includedNodes is not null)
        {
            children = children
                .Where(entry => includedNodes.Contains(entry.DisplayPath))
                .ToList();
        }

        if (query.AnnotationKind is not null)
        {
            var annotationMetrics = filterMetrics is not null
                && filterMetrics.Kind == query.AnnotationKind.Value
                    ? filterMetrics
                    : await _metricsService.GetMetricsAsync(
                        configId,
                        workspace,
                        solution,
                        query.AnnotationKind.Value,
                        cancellationToken);

            children = children
                .Select(entry => ApplyAnnotation(entry, query.AnnotationKind.Value, annotationMetrics))
                .ToList();
        }
        else
        {
            children = children
                .Select(entry => entry with { AnnotationKind = null, AnnotationValue = null })
                .ToList();
        }

        return children.ToPagedResult(paging);
    }

    private static bool HasFilter(CodeTreeQuery query)
    {
        return query.FilterMetric is not null
               && query.FilterOperator is not null
               && query.FilterThreshold is not null;
    }

    private static CodeTreeEntryDto ApplyAnnotation(
        CodeTreeEntryDto entry,
        CodeTreeAnnotationKind kind,
        CodeTreeMetricValues metrics)
    {
        if (entry.Type is CodeTreeEntryType.Type or CodeTreeEntryType.Member
            && (kind is CodeTreeAnnotationKind.FileAuthorCount or CodeTreeAnnotationKind.FileChangeCount))
        {
            return entry with { AnnotationKind = null, AnnotationValue = null };
        }

        var value = GetAnnotationValue(entry, kind, metrics);

        return entry with { AnnotationKind = kind, AnnotationValue = value };
    }

    private static int GetAnnotationValue(
        CodeTreeEntryDto entry,
        CodeTreeAnnotationKind kind,
        CodeTreeMetricValues metrics)
    {
        if (entry.Type is CodeTreeEntryType.Type && !string.IsNullOrWhiteSpace(entry.DocumentationId))
        {
            return metrics.TypeValuesByDocId.TryGetValue(entry.DocumentationId, out var typeValue)
                ? typeValue
                : 0;
        }

        if (entry.Type is CodeTreeEntryType.Member && !string.IsNullOrWhiteSpace(entry.DocumentationId))
        {
            return metrics.MemberValuesByDocId.TryGetValue(entry.DocumentationId, out var memberValue)
                ? memberValue
                : 0;
        }

        return metrics.NodeValues.TryGetValue(entry.DisplayPath, out var stored)
            ? stored
            : 0;
    }

    private static HashSet<string> BuildIncludedSet(
        GraphSnapshot snapshot,
        IReadOnlyDictionary<string, int> fileValues,
        CodeTreeFilterOperator filterOperator,
        int threshold)
    {
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (fileValues.Count == 0 || snapshot.Nodes.Count == 0)
        {
            return included;
        }

        var parentsByChild = BuildParentMap(snapshot.Edges);
        var nodesById = snapshot.Nodes.ToDictionary(n => n.Id);

        var fileNodeIdsByRepositoryPath = snapshot.Nodes
            .Where(node => node.Kind == GraphNodeKind.File
                           && node.Attributes?.TryGetValue("RepositoryPath", out _) == true)
            .SelectMany(node =>
            {
                var repoPath = node.Attributes!["RepositoryPath"];
                var normalized = NormalizePath(repoPath);
                var display = node.Attributes!.GetValueOrDefault("DisplayPath");
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    repoPath,
                    normalized
                };

                if (!string.IsNullOrWhiteSpace(display))
                {
                    keys.Add(display!);
                    keys.Add(NormalizePath(display!));
                }

                return keys.Select(key => new KeyValuePair<string, Guid>(key, node.Id));
            })
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

        foreach (var (repositoryPath, value) in fileValues)
        {
            var matches = filterOperator switch
            {
                CodeTreeFilterOperator.LessThanOrEqual => value <= threshold,
                CodeTreeFilterOperator.GreaterThan => value > threshold,
                _ => false
            };

            if (!matches)
            {
                continue;
            }

            var normalizedPath = NormalizePath(repositoryPath);
            if (!fileNodeIdsByRepositoryPath.TryGetValue(repositoryPath, out var nodeId)
                && !fileNodeIdsByRepositoryPath.TryGetValue(normalizedPath, out nodeId))
            {
                continue;
            }

            AddWithAncestors(included, nodeId, parentsByChild, nodesById);
        }

        return included;
    }

    private static void AddWithAncestors(
        ISet<string> included,
        Guid nodeId,
        IReadOnlyDictionary<Guid, List<Guid>> parentsByChild,
        IReadOnlyDictionary<Guid, GraphNodeDto> nodesById)
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
                included.Add(key);
            }

            if (parentsByChild.TryGetValue(current, out var parents))
            {
                foreach (var parent in parents)
                {
                    queue.Enqueue(parent);
                }
            }
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

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('.');
        normalized = normalized.TrimStart('/');
        return normalized;
    }
}
