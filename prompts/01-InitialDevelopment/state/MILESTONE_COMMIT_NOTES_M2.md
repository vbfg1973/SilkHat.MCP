M2: repository config/group persistence + dashboard CRUD

Summary:
- Add EF Core SQLite persistence with repository config/group entities and DbContext.
- Implement CRUD endpoints for repository configs and groups with validation and ProblemDetails.
- Update MudBlazor dashboard to list/create/edit configs and groups.
- Persist SQLite DB via Docker volume and document the storage location.
- Fix API Dockerfile restore inputs to include referenced projects.

Key files:
- src/SilkHat.Infrastructure/SilkHatDbContext.cs
- src/SilkHat.Infrastructure/Entities/RepositoryConfig.cs
- src/SilkHat.Infrastructure/Entities/RepositoryGroup.cs
- src/SilkHat.Core/Dtos/RepositoryConfigDto.cs
- src/SilkHat.Core/Dtos/RepositoryGroupDto.cs
- src/SilkHat.Api/Program.cs
- src/SilkHat.Api/Dockerfile
- src/SilkHat.Ui/Pages/Home.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- docker-compose.yml
- docs/README.md

Notes:
- DB defaults to `./silkhat.db` locally and `/data/silkhat.db` in Docker via the `silkhat-data` volume.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
