using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Tests.TestHelpers
{
    public static class CodeWorkspaceFactory
    {
        public const string DefaultSolutionId = "solution-1";
        private static readonly Lazy<CodeRepositoryWorkspace> CachedWorkspace = new(CreateWorkspaceInternal, true);

        public static CodeRepositoryWorkspace CreateWorkspace()
        {
            return CachedWorkspace.Value;
        }

        private static CodeRepositoryWorkspace CreateWorkspaceInternal()
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
                new("sym-alpha", null, "Foo", "Alpha", "Alpha.Foo", "alpha", "Alpha", NamedTypeKind.Class, false,
                    "./Alpha/Foo.cs"),
                new("sym-beta", null, "Bar", "Alpha.Sub", "Alpha.Sub.Bar", "beta", "Beta", NamedTypeKind.Struct, false,
                    "./Beta/Bar.cs")
            };

            var namedTypesByKey = namedTypes.ToDictionary(type => type.SymbolKey, type => type);
            var namedTypesByDocId = new Dictionary<string, NamedTypeDto>(StringComparer.Ordinal);

            var solution = new CodeSolutionWorkspace(
                DefaultSolutionId,
                "Repo",
                "/repo/Repo.sln",
                "./Repo.sln",
                projects,
                new List<CodeTreeEntryDto>(),
                new Dictionary<string, IReadOnlyList<CodeTreeEntryDto>>(StringComparer.OrdinalIgnoreCase),
                namespaces,
                namedTypes,
                namedTypesByKey,
                namedTypesByDocId,
                new Dictionary<string, Compilation>());

            return new CodeRepositoryWorkspace(
                "/repo",
                new Dictionary<string, CodeSolutionWorkspace>
                {
                    [DefaultSolutionId] = solution
                });
        }
    }
}