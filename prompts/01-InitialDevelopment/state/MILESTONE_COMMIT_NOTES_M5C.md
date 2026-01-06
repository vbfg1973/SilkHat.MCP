M5c: Repository group loader UI + solution selection

Summary:
- Replaced the dashboard repository/group/config UI with a new repository group loader component that manages group creation, repository selection, and solution enablement.
- Added solution selection persistence via repository config solutions and a discovery endpoint for listing repo solution files.
- Group loads now clear existing loaded repositories, workspaces, and git caches before reloading enabled solutions.
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
- src/SilkHat.Infrastructure/SilkHatDbContext.cs
- src/SilkHat.Api/Controllers/RepositoryDiscoveryController.cs
- src/SilkHat.Api/Controllers/RepositoryConfigsController.cs
- src/SilkHat.Api/Controllers/RepositoryGroupsController.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
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

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker compose up --build -d
