using SilkHat.Api.Extensions;
using SilkHat.Api.Models;
using SilkHat.Code.Analysis.Abstractions;
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

    public CodeTreeQueryService(
        ICodeTreeService treeService,
        ICodeTreeMetricsService metricsService)
    {
        _treeService = treeService;
        _metricsService = metricsService;
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

            includedNodes = BuildIncludedSet(
                solution.TreeEntries,
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
        IReadOnlyList<CodeTreeEntryDto> entries,
        IReadOnlyDictionary<string, int> fileValues,
        CodeTreeFilterOperator filterOperator,
        int threshold)
    {
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.Where(entry => entry.Type == CodeTreeEntryType.File))
        {
            var value = fileValues.TryGetValue(entry.DisplayPath, out var stored)
                ? stored
                : 0;

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

            AddWithAncestors(included, entry.DisplayPath);
        }

        return included;
    }

    private static void AddWithAncestors(ISet<string> included, string displayPath)
    {
        if (!included.Add(displayPath))
        {
            return;
        }

        var parent = GetParentDisplayPath(displayPath);
        while (!string.IsNullOrEmpty(parent))
        {
            included.Add(parent);
            parent = GetParentDisplayPath(parent);
        }
    }

    private static string GetParentDisplayPath(string displayPath)
    {
        if (string.IsNullOrWhiteSpace(displayPath))
        {
            return string.Empty;
        }

        var lastSeparator = displayPath.LastIndexOf('/');
        return lastSeparator <= 0 ? string.Empty : displayPath[..lastSeparator];
    }
}
