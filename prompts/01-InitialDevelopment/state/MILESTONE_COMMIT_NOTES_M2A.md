M2a: convert API endpoints to controllers

Summary:
- Replace minimal API endpoints with controller-based endpoints for health, repository configs, and groups.
- Preserve ProblemDetails with correlationId/category via shared controller base.
- Centralize root path normalization and optional string trimming helpers.
- Wire controllers in startup and remove minimal API mappings.

Key files:
- src/SilkHat.Api/Program.cs
- src/SilkHat.Api/Controllers/ApiControllerBase.cs
- src/SilkHat.Api/Controllers/HealthController.cs
- src/SilkHat.Api/Controllers/RepositoryConfigsController.cs
- src/SilkHat.Api/Controllers/RepositoryGroupsController.cs
- src/SilkHat.Api/Extensions/RepositoryInputNormalization.cs

Notes:
- All future endpoints should be implemented as controllers only.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
