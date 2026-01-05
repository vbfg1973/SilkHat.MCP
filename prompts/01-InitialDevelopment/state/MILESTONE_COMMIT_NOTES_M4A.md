M4a: Testing standardization for controllers and services

Summary:
- Added interfaces for repository and code workspace stores to enable dependency-call verification in controller tests.
- Added comprehensive controller tests for all endpoints with response and dependency assertions.
- Added unit and integration tests for services (repository store, code workspace store, repo command processor, code workspace loader, SymbolKey helper).
- Hardened SymbolKey resolution with a fallback when reflection support is unavailable.
- Updated Docker port mappings and nginx root to make UI/API reachable on non-conflicting host ports.

Key files:
- src/SilkHat.Analysis/Abstractions/ILoadedRepositoryStore.cs
- src/SilkHat.Code.Analysis/Abstractions/ICodeWorkspaceStore.cs
- src/SilkHat.Analysis/Models/RepoCommandContext.cs
- src/SilkHat.Code.Analysis/Services/SymbolKeyUtility.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
- src/SilkHat.Api/Controllers/CodeProjectsController.cs
- src/SilkHat.Api/Controllers/CodeNamespacesController.cs
- src/SilkHat.Api/Controllers/CodeNamedTypesController.cs
- src/SilkHat.Api/Controllers/CodeSymbolsController.cs
- tests/SilkHat.Api.Tests/Controllers/*.cs
- tests/SilkHat.Tests/Services/*.cs
- docker-compose.yml
- src/SilkHat.Ui/nginx.conf
- src/SilkHat.Ui/wwwroot/appsettings.Development.json

Notes:
- API now binds externally on `http://localhost:18080`, UI on `http://localhost:10080` via docker-compose.
- SymbolKey fallback uses fully-qualified display string when reflection lookup is unavailable.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
