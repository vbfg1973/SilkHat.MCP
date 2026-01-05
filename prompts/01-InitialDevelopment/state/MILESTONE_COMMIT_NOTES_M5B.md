M5b: Repository root discovery + group loads

Summary:
- Added repository discovery under REPO_ROOT with fast git validation and new API endpoint for available repositories.
- Enforced repository path validation under REPO_ROOT for config create/update and exposed loaded-only repository listings for the IDE.
- Added repository group load endpoint and UI support for group loads and available repo browsing.
- Added unit/controller/UI tests for discovery, validation, loaded filtering, and group load behavior.
- Docker compose now uses an externally provided REPO_ROOT host path for the /repos bind mount.

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
- Set REPO_ROOT to a host path before running compose; it is bind-mounted to /repos in the API container.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- REPO_ROOT=/path/to/repos docker compose up --build -d
