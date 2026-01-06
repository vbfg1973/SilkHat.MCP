M5e: PostgreSQL migration + docker compose refinement

Summary:
- migrate EF Core configuration to PostgreSQL and align migrations with Postgres column types
- add PostgreSQL + pgAdmin services in docker compose with environment-driven connection strings
- add opt-in integration test project using Testcontainers for real database coverage

Key files:
- src/SilkHat.Api/Program.cs
- src/SilkHat.Api/appsettings.json
- src/SilkHat.Api/SilkHat.Api.csproj
- src/SilkHat.Infrastructure/Migrations/20260106081500_InitialCreate.cs
- src/SilkHat.Infrastructure/Migrations/20260106081500_InitialCreate.Designer.cs
- src/SilkHat.Infrastructure/Migrations/20260106081530_AddRepositorySolutionConfigs.cs
- src/SilkHat.Infrastructure/Migrations/20260106081530_AddRepositorySolutionConfigs.Designer.cs
- src/SilkHat.Infrastructure/Migrations/SilkHatDbContextModelSnapshot.cs
- docker-compose.yml
- docker/pgadmin/servers.json
- tests/SilkHat.IntegrationTests/SilkHat.IntegrationTests.csproj
- tests/SilkHat.IntegrationTests/Infrastructure/PostgresContainerFixture.cs
- tests/SilkHat.IntegrationTests/RepositoryPersistenceTests.cs

Notes:
- Integration tests are opt-in via explicit `dotnet test tests/SilkHat.IntegrationTests/SilkHat.IntegrationTests.csproj`.
- pgAdmin login: admin@example.com / admin (http://localhost:10081).

Tests:
- dotnet test (fails in this environment: MSBuild named pipe SocketException 13: permission denied)
 - dotnet test tests/SilkHat.IntegrationTests/SilkHat.IntegrationTests.csproj (requires Docker + Testcontainers)

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker compose up --build -d (fails here: cannot access /var/run/docker.sock)
