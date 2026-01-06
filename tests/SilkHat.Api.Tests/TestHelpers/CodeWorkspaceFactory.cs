using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.TestHelpers;

public static class CodeWorkspaceFactory
{
    public static CodeRepositoryWorkspace CreateWorkspace()
    {
        var projectAlpha = new ProjectIndex(
            "alpha",
            "Alpha",
            "C#",
            "Alpha",
            new List<CodeProjectReferenceDto>
            {
                new("beta", "Beta")
            },
            new List<CodeProjectReferenceDto>());

        var projectBeta = new ProjectIndex(
            "beta",
            "Beta",
            "C#",
            "Beta",
            new List<CodeProjectReferenceDto>(),
            new List<CodeProjectReferenceDto>
            {
                new("alpha", "Alpha")
            });

        var projects = new Dictionary<string, ProjectIndex>
        {
            ["alpha"] = projectAlpha,
            ["beta"] = projectBeta
        };

        var namespaces = new List<string> { "Alpha", "Alpha.Sub" };

        var namedTypes = new List<NamedTypeDto>
        {
            new("sym-alpha", "Foo", "Alpha", "Alpha.Foo", "alpha", "Alpha", NamedTypeKind.Class, false, "./Alpha/Foo.cs"),
            new("sym-beta", "Bar", "Alpha.Sub", "Alpha.Sub.Bar", "beta", "Beta", NamedTypeKind.Struct, false, "./Beta/Bar.cs")
        };

        var namedTypesByKey = namedTypes.ToDictionary(type => type.SymbolKey, type => type);

        return new CodeRepositoryWorkspace(
            "/repo",
            new List<Workspace>(),
            projects,
            new List<CodeTreeEntryDto>(),
            namespaces,
            namedTypes,
            namedTypesByKey,
            new Dictionary<string, Compilation>());
    }
}
