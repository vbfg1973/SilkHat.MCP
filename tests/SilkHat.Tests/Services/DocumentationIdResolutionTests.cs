using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services
{
    public sealed class DocumentationIdResolutionTests
    {
        [Fact]
        public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForMethods()
        {
            var (_, firstSolution) = await LoadWorkspaceAsync();
            var docIds = new[]
            {
                GetMethodDocId(firstSolution, "SilkHat.Sample.Lib.IClock", "get_Now"),
                GetMethodDocId(firstSolution, "SilkHat.Sample.Lib.SystemClock", "get_Now"),
                GetMethodDocId(firstSolution, "SilkHat.Sample.App.IGreetingProvider", "GetGreeting"),
                GetMethodDocId(firstSolution, "SilkHat.Sample.App.FriendlyGreetingProvider", "GetGreeting"),
                GetMethodDocId(firstSolution, "SilkHat.Sample.App.PublicEntry", "Run")
            };

            var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
            foreach (var docId in docIds)
            {
                var resolved = DocumentationIdUtility.FindMethodByDocumentationId(secondSolution, docId);
                Assert.NotNull(resolved);
                Assert.Equal(docId, resolved!.GetDocumentationCommentId());
            }
        }

        [Fact]
        public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForTypes()
        {
            var (_, firstSolution) = await LoadWorkspaceAsync();
            var docIds = new[]
            {
                GetTypeDocId(firstSolution, "SilkHat.Sample.Lib.IClock"),
                GetTypeDocId(firstSolution, "SilkHat.Sample.Lib.SystemClock"),
                GetTypeDocId(firstSolution, "SilkHat.Sample.App.FriendlyGreetingProvider"),
                GetTypeDocId(firstSolution, "SilkHat.Sample.App.PublicEntry")
            };

            var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
            foreach (var docId in docIds)
            {
                var resolved = DocumentationIdUtility.FindTypeByDocumentationId(secondSolution, docId);
                Assert.NotNull(resolved);
                Assert.Equal(docId, resolved!.GetDocumentationCommentId());
            }
        }

        [Fact]
        public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForPropertiesFieldsAndEvents()
        {
            var (firstWorkspace, firstSolution) = await LoadWorkspaceAsync();
            var docIds = new[]
            {
                GetPropertyDocId(firstSolution, "SilkHat.Sample.App.StatusHelper", "Grade11PlusScore"),
                GetFieldDocId(firstSolution, "SilkHat.Sample.Lib.LibConstants", "DefaultLabel"),
                GetEventDocId(firstSolution, "SilkHat.Sample.App.StatusHelper", "StatusChecked")
            };

            var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
            foreach (var docId in docIds)
                if (docId.StartsWith("P:", StringComparison.Ordinal))
                {
                    var resolved = DocumentationIdUtility.FindPropertyByDocumentationId(secondSolution, docId);
                    Assert.NotNull(resolved);
                }
                else if (docId.StartsWith("F:", StringComparison.Ordinal))
                {
                    var resolved = DocumentationIdUtility.FindFieldByDocumentationId(secondSolution, docId);
                    Assert.NotNull(resolved);
                }
                else if (docId.StartsWith("E:", StringComparison.Ordinal))
                {
                    var resolved = DocumentationIdUtility.FindEventByDocumentationId(secondSolution, docId);
                    Assert.NotNull(resolved);
                }
        }

        [Fact]
        public async Task DocumentationIds_ResolveAcrossWorkspaceReloads_ForNamespaces()
        {
            var (firstWorkspace, firstSolution) = await LoadWorkspaceAsync();
            var docId = GetNamespaceDocId(firstSolution, "SilkHat.Sample.App");

            var (secondWorkspace, secondSolution) = await LoadWorkspaceAsync();
            var resolved = DocumentationIdUtility.FindNamespaceByDocumentationId(secondSolution, docId);
            Assert.NotNull(resolved);
            Assert.Equal(docId, resolved!.GetDocumentationCommentId());
        }

        private static string GetMethodDocId(CodeSolutionWorkspace solution, string typeMetadataName, string methodName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName(typeMetadataName);
                if (type is null) continue;

                var method = type.GetMembers()
                    .OfType<IMethodSymbol>()
                    .First(member => member.Name == methodName);
                var docId = DocumentationIdUtility.GetDocumentationId(method);
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Method {typeMetadataName}.{methodName} not found.");
        }

        private static string GetTypeDocId(CodeSolutionWorkspace solution, string typeMetadataName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName(typeMetadataName);
                if (type is null) continue;

                var docId = type.GetDocumentationCommentId();
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Type {typeMetadataName} not found.");
        }

        private static string GetPropertyDocId(CodeSolutionWorkspace solution, string typeMetadataName,
            string propertyName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName(typeMetadataName);
                if (type is null) continue;

                var property = type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .First(member => member.Name == propertyName);
                var docId = DocumentationIdUtility.GetDocumentationId(property);
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Property {typeMetadataName}.{propertyName} not found.");
        }

        private static string GetFieldDocId(CodeSolutionWorkspace solution, string typeMetadataName, string fieldName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName(typeMetadataName);
                if (type is null) continue;

                var field = type.GetMembers()
                    .OfType<IFieldSymbol>()
                    .First(member => member.Name == fieldName);
                var docId = DocumentationIdUtility.GetDocumentationId(field);
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Field {typeMetadataName}.{fieldName} not found.");
        }

        private static string GetEventDocId(CodeSolutionWorkspace solution, string typeMetadataName, string eventName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var type = compilation.GetTypeByMetadataName(typeMetadataName);
                if (type is null) continue;

                var @event = type.GetMembers()
                    .OfType<IEventSymbol>()
                    .First(member => member.Name == eventName);
                var docId = DocumentationIdUtility.GetDocumentationId(@event);
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Event {typeMetadataName}.{eventName} not found.");
        }

        private static string GetNamespaceDocId(CodeSolutionWorkspace solution, string namespaceName)
        {
            foreach (var compilation in solution.Compilations.Values)
            {
                var @namespace = FindNamespace(compilation.GlobalNamespace, namespaceName);
                if (@namespace is null) continue;

                var docId = DocumentationIdUtility.GetDocumentationId(@namespace);
                if (!string.IsNullOrWhiteSpace(docId)) return docId;
            }

            throw new InvalidOperationException($"Namespace {namespaceName} not found.");
        }

        private static INamespaceSymbol? FindNamespace(INamespaceSymbol root, string fullName)
        {
            if (string.Equals(root.ToDisplayString(), fullName, StringComparison.Ordinal)) return root;

            foreach (var member in root.GetMembers().OfType<INamespaceSymbol>())
            {
                var match = FindNamespace(member, fullName);
                if (match is not null) return match;
            }

            return null;
        }

        private static async Task<(CodeRepositoryWorkspace Workspace, CodeSolutionWorkspace Solution)>
            LoadWorkspaceAsync()
        {
            var sampleRoot = LocateSampleRoot();
            var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
            var workspace = await loader.LoadAsync(
                sampleRoot,
                new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
                CancellationToken.None);
            var solution = Assert.Single(workspace.Solutions.Values);
            return (workspace, solution);
        }

        private static string LocateSampleRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "samples", "solution01", "SilkHat.Sample.sln");
                if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate samples/solution01/SilkHat.Sample.sln.");
        }
    }
}