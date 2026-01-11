using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Graph;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeTreeService : ICodeTreeService
{
    private readonly IGraphStoreProvider _graphStoreProvider;

    public CodeTreeService(IGraphStoreProvider graphStoreProvider)
    {
        _graphStoreProvider = graphStoreProvider;
    }

    public Task<IReadOnlyList<CodeTreeEntryDto>> GetTreeAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string? parentId,
        CancellationToken cancellationToken)
    {
        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        var store = _graphStoreProvider.GetOrAdd(solution.SolutionId);
        var parentKey = string.IsNullOrWhiteSpace(parentId) ? solution.SolutionId : parentId.Trim().TrimEnd('/');

        if (!store.TryGetNodeByKey(parentKey, out var parentNode) || parentNode is null)
        {
            return Task.FromResult<IReadOnlyList<CodeTreeEntryDto>>(Array.Empty<CodeTreeEntryDto>());
        }

        var edgeType = parentNode.Kind switch
        {
            GraphNodeKind.File => EdgeType.DeclaresType,
            GraphNodeKind.NamedType => EdgeType.DeclaresMember,
            GraphNodeKind.Method => EdgeType.DeclaresParameter,
            _ => EdgeType.Contains
        };

        var edges = store.GetOutEdges(parentNode.Id, edgeType);

        var children = edges
            .Select(edge =>
            {
                store.TryGetNode(edge.TargetId, out var node);
                return node;
            })
            .Where(node => node is not null)
            .Select(node => MapNodeToEntry(node!, solution))
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .OrderBy(entry => GetNodeSortOrder(entry.Type))
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<CodeTreeEntryDto>>(children);
    }

    private static CodeTreeEntryDto? MapNodeToEntry(GraphNodeDto node, CodeSolutionWorkspace solution)
    {
        var attributes = node.Attributes ?? new Dictionary<string, string>();
        var projectKey = attributes.TryGetValue("ProjectKey", out var pk)
            ? pk
            : solution.Projects.Values.FirstOrDefault()?.ProjectKey ?? string.Empty;
        var projectName = attributes.TryGetValue("ProjectName", out var pn)
            ? pn
            : solution.Projects.TryGetValue(projectKey, out var project)
                ? project.Name
                : solution.Projects.Values.FirstOrDefault()?.Name ?? string.Empty;
        var location = BuildLocation(attributes);

        return node.Kind switch
        {
            GraphNodeKind.Project => new CodeTreeEntryDto(
                node.Key,
                node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.Project,
                projectKey,
                projectName,
                null,
                null,
                null,
                location,
                null,
                null),

            GraphNodeKind.Folder => new CodeTreeEntryDto(
                attributes.TryGetValue("RepositoryPath", out var repo) ? repo : node.Key,
                attributes.TryGetValue("DisplayPath", out var disp) ? disp : node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.Directory,
                projectKey,
                projectName,
                null,
                null,
                null,
                location,
                null,
                null),

            GraphNodeKind.File => new CodeTreeEntryDto(
                attributes.TryGetValue("RepositoryPath", out var repo) ? repo : node.Key,
                attributes.TryGetValue("DisplayPath", out var disp) ? disp : node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.File,
                projectKey,
                projectName,
                null,
                null,
                null,
                location,
                null,
                null),

            GraphNodeKind.NamedType => new CodeTreeEntryDto(
                attributes.TryGetValue("RepositoryPath", out var repo) ? repo : string.Empty,
                node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.Type,
                projectKey,
                projectName,
                attributes.TryGetValue("DocumentationId", out var doc) ? doc : node.Key,
                "NamedType",
                attributes.TryGetValue("RealType", out var rt) ? rt : null,
                location,
                null,
                null),

            GraphNodeKind.Field or GraphNodeKind.Property or GraphNodeKind.Method => new CodeTreeEntryDto(
                attributes.TryGetValue("RepositoryPath", out var repo) ? repo : string.Empty,
                node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.Member,
                projectKey,
                projectName,
                attributes.TryGetValue("DocumentationId", out var memberDoc) ? memberDoc : node.Key,
                "Member",
                attributes.TryGetValue("RealType", out var rtMember) ? rtMember : node.Kind.ToString(),
                location,
                null,
                null),

            GraphNodeKind.Parameter => new CodeTreeEntryDto(
                attributes.TryGetValue("RepositoryPath", out var repo) ? repo : string.Empty,
                node.Key,
                node.Label ?? node.Key,
                CodeTreeEntryType.Member,
                projectKey,
                projectName,
                attributes.TryGetValue("DocumentationId", out var paramDoc) ? paramDoc : node.Key,
                "Parameter",
                attributes.TryGetValue("RealType", out var paramType) ? paramType : "Parameter",
                location,
                null,
                null),

            _ => null
        };
    }

    private static int GetNodeSortOrder(CodeTreeEntryType type)
    {
        return type switch
        {
            CodeTreeEntryType.Project => 0,
            CodeTreeEntryType.Directory => 1,
            CodeTreeEntryType.File => 2,
            CodeTreeEntryType.Type => 3,
            CodeTreeEntryType.Member => 4,
            _ => 5
        };
    }

    private static CodeLocationDto? BuildLocation(IReadOnlyDictionary<string, string> attributes)
    {
        if (!attributes.TryGetValue("FilePath", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!attributes.TryGetValue("SpanStart", out var spanStartStr) ||
            !attributes.TryGetValue("SpanLength", out var spanLengthStr) ||
            !int.TryParse(spanStartStr, out var spanStart) ||
            !int.TryParse(spanLengthStr, out var spanLength))
        {
            return null;
        }

        if (!attributes.TryGetValue("StartLine", out var startLineStr) ||
            !attributes.TryGetValue("StartColumn", out var startColumnStr) ||
            !attributes.TryGetValue("EndLine", out var endLineStr) ||
            !attributes.TryGetValue("EndColumn", out var endColumnStr) ||
            !int.TryParse(startLineStr, out var startLine) ||
            !int.TryParse(startColumnStr, out var startColumn) ||
            !int.TryParse(endLineStr, out var endLine) ||
            !int.TryParse(endColumnStr, out var endColumn))
        {
            return null;
        }

        return new CodeLocationDto(
            path,
            new CodeTextSpanDto(spanStart, spanLength),
            new CodeLineSpanDto(startLine, startColumn, endLine, endColumn));
    }

    private static int GetMemberOrder(CodeTreeEntryDto entry)
    {
        return entry.RealType?.ToLowerInvariant() switch
        {
            "field" => 0,
            "property" => 1,
            "event" => 2,
            "constructor" => 3,
            "method" => 4,
            _ => 5
        };
    }

}
