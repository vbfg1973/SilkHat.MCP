using Microsoft.CodeAnalysis;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Code.Analysis.Models;

public sealed record ProjectIndex(
    string ProjectKey,
    string Name,
    string Language,
    string AssemblyName,
    IReadOnlyList<CodeProjectReferenceDto> References,
    IReadOnlyList<CodeProjectReferenceDto> ReferencedBy);

public sealed record CodeSolutionWorkspace(
    string SolutionId,
    string SolutionName,
    string SolutionPath,
    string RelativePath,
    IReadOnlyDictionary<string, ProjectIndex> Projects,
    IReadOnlyList<CodeTreeEntryDto> TreeEntries,
    IReadOnlyDictionary<string, IReadOnlyList<CodeTreeEntryDto>> TreeChildrenByParent,
    IReadOnlyList<string> Namespaces,
    IReadOnlyList<NamedTypeDto> NamedTypes,
    IReadOnlyDictionary<string, NamedTypeDto> NamedTypesBySymbolKey,
    IReadOnlyDictionary<string, NamedTypeDto> NamedTypesByDocId,
    IReadOnlyDictionary<string, Compilation> Compilations);

public sealed class CodeRepositoryWorkspace
{
    public CodeRepositoryWorkspace(
        string rootPath,
        IReadOnlyDictionary<string, CodeSolutionWorkspace> solutions)
    {
        RootPath = rootPath;
        Solutions = solutions;
    }

    public string RootPath { get; }
    public IReadOnlyDictionary<string, CodeSolutionWorkspace> Solutions { get; }

    public CodeSolutionWorkspace? TryGetSolution(string solutionId)
        => Solutions.TryGetValue(solutionId, out var solution) ? solution : null;
}
