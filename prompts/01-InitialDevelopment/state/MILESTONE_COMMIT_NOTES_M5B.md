M5b: Repository root discovery + group loads

Summary:
- Added repository discovery under REPO_ROOT with fast git validation and new API endpoint for available repositories.
- Enforced repository path validation under REPO_ROOT for config create/update and exposed loaded-only repository listings for the IDE.
- Added repository group load endpoint and UI support for group loads and available repo browsing.
- Added unit/controller/UI tests for discovery, validation, loaded filtering, and group load behavior.
- Added REPO_ROOT and /repos volume mapping to docker compose (create ./repos on host).

Key files:
- src/SilkHat.Analysis/Services/RepositoryDiscoveryService.cs
- src/SilkHat.Analysis/Abstractions/IRepositoryDiscoveryService.cs
- src/SilkHat.Api/Controllers/RepositoryDiscoveryController.cs
- src/SilkHat.Api/Controllers/RepositoryConfigsController.cs
- src/SilkHat.Api/Controllers/RepositoryGroupsController.cs
- src/SilkHat.Ui/Pages/Home.razor
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- docker-compose.yml
- prompts/01-InitialDevelopment/08_API_SURFACE.md
- prompts/01-InitialDevelopment/10_DOCKER_AND_COMPOSE.md
- tests/SilkHat.Tests/Services/RepositoryDiscoveryServiceTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryDiscoveryControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryConfigsControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/RepositoryGroupsControllerTests.cs
- tests/SilkHat.Ui.Tests/Pages/HomeTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs

Notes:
- Ensure the host `./repos` directory exists for the docker bind mount.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
