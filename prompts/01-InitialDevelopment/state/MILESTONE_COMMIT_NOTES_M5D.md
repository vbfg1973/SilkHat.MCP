M5d: Code analysis without Buildalyzer

Summary:
- replace Buildalyzer with .sln/.csproj parsing and in-process Roslyn workspace construction
- parse project references, package references, compile items, and constants for analysis inputs
- add parser-focused tests for sample solution and explicit project parsing scenarios
- add project-based code tree entries from loaded solutions and expose them via a new code tree endpoint
- update IDE UI to use the code tree for repository navigation while keeping git history lookups

Key files:
- src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs
- src/SilkHat.Code.Analysis/Services/SolutionParser.cs
- src/SilkHat.Code.Analysis/Services/ProjectParser.cs
- src/SilkHat.Code.Analysis/Models/ParsedSolution.cs
- src/SilkHat.Code.Analysis/Models/ParsedProject.cs
- src/SilkHat.Code.Analysis/SilkHat.Code.Analysis.csproj
- tests/SilkHat.Tests/Services/SolutionParserTests.cs
- tests/SilkHat.Tests/Services/ProjectParserTests.cs
- src/SilkHat.Code.Core/Dtos/CodeTreeDtos.cs
- src/SilkHat.Api/Controllers/CodeTreeController.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Ui/Models/CodeTreeModels.cs
- tests/SilkHat.Api.Tests/Controllers/CodeTreeControllerTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs

Notes:
- Buildalyzer dependencies removed; compilation now uses AdhocWorkspace + runtime metadata references.
- Roslyn workspace package pinned to 4.8.0 to avoid NuGet version conflicts with EF Core Design.
- Added Microsoft.CodeAnalysis.CSharp to satisfy CSharpParseOptions usage in the new loader.
- Added Microsoft.Extensions.Options to SilkHat.Analysis to fix missing IOptions<> build errors in publish.
- Added Microsoft.CodeAnalysis.CSharp.Workspaces to ensure C# language services are available in AdhocWorkspace.
- Added git installation in the API Dockerfile to allow git CLI calls at runtime.

Tests:
- dotnet test (fails in this environment: MSBuild named pipe SocketException 13: permission denied)

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker compose up --build -d (fails here: cannot access /var/run/docker.sock)
