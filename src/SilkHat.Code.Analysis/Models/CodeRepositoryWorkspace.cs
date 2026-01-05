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

public sealed class CodeRepositoryWorkspace
{
    public CodeRepositoryWorkspace(
        string rootPath,
        IReadOnlyList<Workspace> workspaces,
        IReadOnlyDictionary<string, ProjectIndex> projects,
        IReadOnlyList<string> namespaces,
        IReadOnlyList<NamedTypeDto> namedTypes,
        IReadOnlyDictionary<string, NamedTypeDto> namedTypesBySymbolKey,
        IReadOnlyDictionary<string, Compilation> compilations)
    {
        RootPath = rootPath;
        Workspaces = workspaces;
        Projects = projects;
        Namespaces = namespaces;
        NamedTypes = namedTypes;
        NamedTypesBySymbolKey = namedTypesBySymbolKey;
        Compilations = compilations;
    }

    public string RootPath { get; }
    public IReadOnlyList<Workspace> Workspaces { get; }
    public IReadOnlyDictionary<string, ProjectIndex> Projects { get; }
    public IReadOnlyList<string> Namespaces { get; }
    public IReadOnlyList<NamedTypeDto> NamedTypes { get; }
    public IReadOnlyDictionary<string, NamedTypeDto> NamedTypesBySymbolKey { get; }
    public IReadOnlyDictionary<string, Compilation> Compilations { get; }
}
