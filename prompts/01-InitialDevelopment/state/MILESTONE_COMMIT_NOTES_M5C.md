M5c: Repository group loader UI + solution selection + migrations

Summary:
- Replaced the dashboard repository/group/config UI with a new repository group loader component that manages group creation, repository selection, and solution enablement.
- Added solution selection persistence via repository config solutions and a discovery endpoint for listing repo solution files.
- Group loads now clear existing loaded repositories, workspaces, and git caches before reloading enabled solutions.
- Switched database initialization to EF Core migrations and split initial schema from repository solution configs.
- Added Serilog logging in API/UI to surface request failures and base URL resolution.
- Defaulted UI API base URL to http://localhost:18080 and enforce API-only resolution (throws on file scheme).
- Added CORS policy allowing UI origin http://localhost:10080 to call the API.
- Moved CORS middleware earlier so error responses include headers.
- Added Buildalyzer error handling to report missing analyzer results with context.
- Adjusted Buildalyzer workspace loading to handle IAnalyzerResults and include project path context.
- Iterate analyzer results when adding to workspaces to avoid API shape mismatches.
- Added Buildalyzer diagnostics logging to surface MSBuild evaluation failures.
- Fixed Buildalyzer logger wrapper to use MSBuild ILogger explicitly.
- Logged MSBuild error/warning events when Buildalyzer returns no analyzer results.
- Forced Buildalyzer to run restore/full build and added result/event count diagnostics.
- Fixed LogDebug overload resolution for Buildalyzer diagnostics.
- Use ILogger.Log with explicit EventId for Buildalyzer summary logging.
- Fixed ILogger.Log formatter signature for Buildalyzer summary logging.
- Fixed BuildEventArguments count logging for Buildalyzer summary.
- Added diagnostic MSBuild console logging and binlog capture for Buildalyzer failures.
- Added diagnostic build event summaries/counts after rerun.
- Added code analysis tests for samples/solution01 with project/type/path assertions.
- Expanded sample-solution tests to include project index and symbol key lookups.
- Extended sample solution with enum/struct/record/delegate types for NamedTypeKind coverage.
- Added a sample library project and reference graph coverage in code analysis tests.
- Updated API/UI models and tests for solution selections, group edits, and new UI flows.

Key files:
- src/SilkHat.Ui/Components/RepositoryGroupLoader.razor
- src/SilkHat.Ui/Pages/Home.razor
- src/SilkHat.Ui/Models/RepositorySolutionModels.cs
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Core/Dtos/RepositorySolutionDtos.cs
- src/SilkHat.Core/Dtos/RepositoryConfigDto.cs
- src/SilkHat.Core/Dtos/RepositoryConfigRequests.cs
- src/SilkHat.Infrastructure/Entities/RepositorySolutionConfig.cs
- src/SilkHat.Infrastructure/Migrations/20260106081500_InitialCreate.cs
- src/SilkHat.Infrastructure/Migrations/20260106081500_InitialCreate.Designer.cs
- src/SilkHat.Infrastructure/Migrations/20260106081530_AddRepositorySolutionConfigs.cs
- src/SilkHat.Infrastructure/Migrations/20260106081530_AddRepositorySolutionConfigs.Designer.cs
- src/SilkHat.Infrastructure/Migrations/SilkHatDbContextModelSnapshot.cs
- src/SilkHat.Infrastructure/SilkHatDbContext.cs
- src/SilkHat.Api/Controllers/RepositoryDiscoveryController.cs
- src/SilkHat.Api/Controllers/RepositoryConfigsController.cs
- src/SilkHat.Api/Controllers/RepositoryGroupsController.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
- src/SilkHat.Api/Program.cs
- src/SilkHat.Ui/Program.cs
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs
- prompts/01-InitialDevelopment/08_API_SURFACE.md
- tests/SilkHat.Api.Tests/Controllers/RepositoryConfigsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryGroupsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryDiscoveryControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryLoadControllerTests.cs
- tests/SilkHat.Ui.Tests/Pages/HomeTests.cs

Notes:
- Solution selections are stored per repository config and must include at least one enabled solution.
- Group loads now reset loaded repository/workspace/git cache state before reloading.
- Repository solution listing uses `GET /api/repositories/available/solutions?path=...`.
- API startup now calls `Database.Migrate()`; migrations must be applied on startup.
- Migrations are code-first (no raw SQL); existing databases must be reset to apply them cleanly.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker compose up --build -d
