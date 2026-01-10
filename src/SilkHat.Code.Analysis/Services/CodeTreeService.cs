using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Services;

public sealed class CodeTreeService : ICodeTreeService
{
    private readonly ICodeSymbolOutlineService _symbolOutlineService;

    public CodeTreeService(ICodeSymbolOutlineService symbolOutlineService)
    {
        _symbolOutlineService = symbolOutlineService;
    }

    public async Task<IReadOnlyList<CodeTreeEntryDto>> GetTreeAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string? parentId,
        CancellationToken cancellationToken)
    {
        if (solution is null)
        {
            throw new ArgumentNullException(nameof(solution));
        }

        var parentKey = string.IsNullOrWhiteSpace(parentId) ? string.Empty : parentId.Trim().TrimEnd('/');
        if (solution.TreeChildrenByParent.TryGetValue(parentKey, out var children))
        {
            return children;
        }

        // If parent is a file, load type nodes from symbol outline
        var fileEntry = solution.TreeEntries.FirstOrDefault(entry =>
            entry.Type == CodeTreeEntryType.File
            && string.Equals(entry.DisplayPath, parentKey, StringComparison.OrdinalIgnoreCase));
        if (fileEntry is not null)
        {
            return await BuildTypeNodesAsync(workspace, solution, fileEntry, cancellationToken);
        }

        // If parent is a type (doc id/symbol key), load member nodes
        if (!string.IsNullOrWhiteSpace(parentKey))
        {
            return await BuildMemberNodesAsync(workspace, solution, parentKey, cancellationToken);
        }

        return Array.Empty<CodeTreeEntryDto>();
    }

    private async Task<IReadOnlyList<CodeTreeEntryDto>> BuildTypeNodesAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        CodeTreeEntryDto fileEntry,
        CancellationToken cancellationToken)
    {
        var outline = await _symbolOutlineService.GetFileSymbolsAsync(
            workspace,
            solution,
            fileEntry.RepositoryPath,
            cancellationToken);

        if (outline.Status != CodeFileSymbolsStatus.Success || outline.Symbols is null)
        {
            return Array.Empty<CodeTreeEntryDto>();
        }

        return outline.Symbols
            .Select(node => CreateTypeEntry(node, fileEntry))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<CodeTreeEntryDto>> BuildMemberNodesAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        string typeIdentifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(typeIdentifier))
        {
            return Array.Empty<CodeTreeEntryDto>();
        }

        var matchingType = solution.NamedTypesByDocId.TryGetValue(typeIdentifier, out var namedType)
            ? namedType
            : solution.NamedTypesBySymbolKey.TryGetValue(typeIdentifier, out var namedTypeByKey)
                ? namedTypeByKey
                : null;
        if (matchingType is null || string.IsNullOrWhiteSpace(matchingType.FilePath))
        {
            return Array.Empty<CodeTreeEntryDto>();
        }

        var projectName = solution.Projects.TryGetValue(matchingType.ProjectKey, out var project)
            ? project.Name
            : matchingType.ProjectKey;
        var parentTypeEntry = new CodeTreeEntryDto(
            matchingType.FilePath,
            typeIdentifier,
            matchingType.Name,
            CodeTreeEntryType.Type,
            matchingType.ProjectKey,
            projectName,
            matchingType.DocumentationId ?? typeIdentifier,
            "NamedType",
            matchingType.Kind.ToString(),
            null,
            null,
            null);

        var outline = await _symbolOutlineService.GetFileSymbolsAsync(
            workspace,
            solution,
            matchingType.FilePath,
            cancellationToken);

        if (outline.Status != CodeFileSymbolsStatus.Success || outline.Symbols is null)
        {
            return Array.Empty<CodeTreeEntryDto>();
        }

        var node = FindNodeByIdentifier(outline.Symbols, typeIdentifier);
        if (node is null)
        {
            return Array.Empty<CodeTreeEntryDto>();
        }

        return node.Children
            .Select(child => CreateChildEntry(child, parentTypeEntry))
            .OrderBy(entry => entry.Type == CodeTreeEntryType.Type ? 0 : 1)
            .ThenBy(entry => GetMemberOrder(entry))
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static SymbolOutlineNodeDto? FindNodeByIdentifier(
        IEnumerable<SymbolOutlineNodeDto> nodes,
        string identifier)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.DocumentationId, identifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.SymbolKey, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindNodeByIdentifier(node.Children, identifier);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static CodeTreeEntryDto CreateTypeEntry(SymbolOutlineNodeDto node, CodeTreeEntryDto fileEntry)
    {
        return new CodeTreeEntryDto(
            fileEntry.RepositoryPath,
            node.DocumentationId ?? node.SymbolKey,
            node.Name,
            CodeTreeEntryType.Type,
            fileEntry.ProjectKey,
            fileEntry.ProjectName,
            node.DocumentationId ?? node.SymbolKey,
            node.SymbolKind,
            node.RealType,
            BuildTreeLocation(node.Location, fileEntry.RepositoryPath),
            null,
            null);
    }

    private static CodeTreeEntryDto CreateChildEntry(
        SymbolOutlineNodeDto node,
        CodeTreeEntryDto parentType)
    {
        var entryType = node.SymbolKind?.Equals("NamedType", StringComparison.OrdinalIgnoreCase) == true
            ? CodeTreeEntryType.Type
            : CodeTreeEntryType.Member;

        return new CodeTreeEntryDto(
            parentType.RepositoryPath,
            node.DocumentationId ?? node.SymbolKey,
            node.Name,
            entryType,
            parentType.ProjectKey,
            parentType.ProjectName,
            node.DocumentationId ?? node.SymbolKey,
            node.SymbolKind,
            node.RealType,
            BuildTreeLocation(node.Location, parentType.RepositoryPath),
            null,
            null);
    }

    private static CodeLocationDto? BuildTreeLocation(CodeLocationDto? location, string repositoryPath)
    {
        if (location is null)
        {
            return null;
        }

        return location with { Path = repositoryPath };
    }
}
