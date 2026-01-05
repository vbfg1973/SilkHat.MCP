M5a: Git history/co-change enhancements + route fixes

Summary:
- Added git change kind, before/after line counts, and total change counts to git history/co-change DTOs.
- Enriched git history parsing with numstat and file line counts per commit.
- Corrected git files routes to use `/git/files/{path}/history` and `/git/files/{path}/cochanges` with URI-encoded paths and decoding on the API.
- Updated UI models, API surface docs, and tests for the new payload shapes and route layout.

Key files:
- src/SilkHat.Git.Core/Dtos/GitHistoryDtos.cs
- src/SilkHat.Git.Analysis/Services/GitCli.cs
- src/SilkHat.Api/Controllers/GitFilesController.cs
- src/SilkHat.Ui/Models/GitModels.cs
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- prompts/01-InitialDevelopment/08_API_SURFACE.md
- tests/SilkHat.Tests/Services/GitCliTests.cs
- tests/SilkHat.Api.Tests/Controllers/GitFilesControllerTests.cs

Notes:
- Git file history/co-change routes now require URI-encoded paths and follow `/git/files/{path}/history` and `/git/files/{path}/cochanges`.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
