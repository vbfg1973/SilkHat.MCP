M5: Git via Process tree/history/co-change

Summary:
- Added git CLI execution, parsing, and caching for tree listing, file history, and co-change stats.
- Exposed git endpoints for tree, history, and co-change with ProblemDetails handling and repository load checks.
- Updated IDE view to render git tree and file history panel.
- Added unit/integration tests for git parsing, controller endpoints, and UI tree rendering.

Key files:
- src/SilkHat.Git.Analysis/Services/GitCommandRunner.cs
- src/SilkHat.Git.Analysis/Services/GitCli.cs
- src/SilkHat.Git.Analysis/Services/GitRepositoryCacheStore.cs
- src/SilkHat.Git.Core/Dtos/GitTreeDtos.cs
- src/SilkHat.Git.Core/Dtos/GitHistoryDtos.cs
- src/SilkHat.Api/Controllers/GitTreeController.cs
- src/SilkHat.Api/Controllers/GitFilesController.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- tests/SilkHat.Tests/Services/GitCliTests.cs
- tests/SilkHat.Api.Tests/Controllers/GitTreeControllerTests.cs
- tests/SilkHat.Api.Tests/Controllers/GitFilesControllerTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs

Notes:
- Git file history/co-change routes use `/git/files/history/{*path}` and `/git/files/cochanges/{*path}` to satisfy ASP.NET catch-all routing constraints.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
