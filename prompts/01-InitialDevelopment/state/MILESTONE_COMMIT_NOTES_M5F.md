M5f: Solution-scoped code endpoints

Summary:
- add solution-scoped code endpoints with stable solution identifiers and solution listing
- update code workspace loader/parser to build per-solution indexes and deterministic solution IDs
- update IDE UI to select solutions and pass solutionId for project/tree requests
- refresh API/UI tests for solution-scoped routes and code workspace usage
- persist solutionId on repository solutions and use it during repository loads and discovery
- update API surface documentation for new solution-scoped routes

Key files:
- src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs
- src/SilkHat.Code.Analysis/Services/SolutionParser.cs
- src/SilkHat.Code.Analysis/Services/SolutionIdentityResolver.cs
- src/SilkHat.Code.Analysis/Models/CodeRepositoryWorkspace.cs
- src/SilkHat.Code.Analysis/Models/SolutionIdentity.cs
- src/SilkHat.Code.Analysis/Models/SolutionReference.cs
- src/SilkHat.Api/Controllers/CodeSolutionsController.cs
- src/SilkHat.Api/Controllers/CodeProjectsController.cs
- src/SilkHat.Api/Controllers/CodeNamespacesController.cs
- src/SilkHat.Api/Controllers/CodeNamedTypesController.cs
- src/SilkHat.Api/Controllers/CodeSymbolsController.cs
- src/SilkHat.Api/Controllers/CodeTreeController.cs
- src/SilkHat.Api/Controllers/RepositoryConfigsController.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
- src/SilkHat.Api/Controllers/RepositoryGroupsController.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Components/RepositoryGroupLoader.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Ui/Models/CodeSolutionModel.cs
- src/SilkHat.Ui/Models/RepositorySolutionModels.cs
- src/SilkHat.Infrastructure/Entities/RepositorySolutionConfig.cs
- src/SilkHat.Infrastructure/Migrations/20260106150930_AddRepositorySolutionIds.cs
- src/SilkHat.Infrastructure/Migrations/20260106150930_AddRepositorySolutionIds.Designer.cs
- src/SilkHat.Infrastructure/Migrations/SilkHatDbContextModelSnapshot.cs
- tests/SilkHat.Api.Tests/Controllers/CodeSolutionsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryConfigsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryLoadControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryGroupsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryDiscoveryControllerTests.cs
- tests/SilkHat.Tests/Services/CodeAnalysisSamplesTests.cs
- tests/SilkHat.Tests/Services/CodeWorkspaceLoaderTests.cs
- tests/SilkHat.Tests/Services/RepoCommandProcessorTests.cs
- tests/SilkHat.Tests/Services/RepositoryDiscoveryServiceTests.cs
- tests/SilkHat.IntegrationTests/RepositoryPersistenceTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs
- tests/SilkHat.Ui.Tests/Pages/HomeTests.cs
- prompts/01-InitialDevelopment/08_API_SURFACE.md

Notes:
- solutionId uses SolutionGuid from the .sln when present; otherwise a deterministic GUID from the normalized relative solution path
- repository solutions now persist solutionId and load flows backfill missing IDs before building workspaces

Tests:
- dotnet test (fails in this environment: MSBuild named pipe SocketException permission denied)

Docker:
- docker compose up --build -d (fails in this environment: Docker daemon socket permission denied; requires REPO_ROOT)
