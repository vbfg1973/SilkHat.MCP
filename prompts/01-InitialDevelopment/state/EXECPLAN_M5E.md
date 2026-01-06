# M5e PostgreSQL Migration + Docker Compose Refinement

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this change, the system stores its data in PostgreSQL rather than SQLite, and developers can inspect and manage data using standard PostgreSQL tools. The API and UI still run via Docker Compose, but Compose now starts PostgreSQL and configures the API to use it, along with pgAdmin for easy database inspection. A user can bring the stack up, load the app, and see repository data persist in PostgreSQL across restarts. A new integration test project uses Testcontainers to run real database tests on demand, without affecting default test runs.

## Progress

- [x] (2026-01-06 14:17Z) Review current persistence configuration, migrations, and Docker Compose wiring.
- [x] (2026-01-06 14:17Z) Decide on PostgreSQL image version and environment variable contract for connection strings.
- [x] (2026-01-06 14:17Z) Update infrastructure project to use Npgsql provider and configure migrations for PostgreSQL.
- [x] (2026-01-06 14:17Z) Update docker-compose to add PostgreSQL service, volumes, and API env configuration.
- [x] (2026-01-06 14:17Z) Add pgAdmin service to docker-compose and ensure it connects to PostgreSQL.
- [x] (2026-01-06 14:17Z) Add a new integration test project using Testcontainers for PostgreSQL-backed tests.
- [x] (2026-01-06 14:17Z) Update tests and configuration to ensure EF Core migrations run against PostgreSQL and integration tests are opt-in.
- [ ] Run `dotnet test` and confirm all non-infrastructure tests pass.
- [ ] Run `docker compose up --build` and confirm API + UI + PostgreSQL run in Docker.
- [x] (2026-01-06 14:46Z) Write Milestone Commit Notes for M5e and update milestone state files.

## Surprises & Discoveries

- Observation: `dotnet test` fails in this environment with MSBuild named pipe permission errors (`SocketException (13): Permission denied`).
  Evidence: MSBuild reports MSB1025 when trying to create the out-of-proc node pipe.
- Observation: `docker compose up --build` fails in this environment because the Docker daemon socket is not accessible.
  Evidence: `dial unix /var/run/docker.sock: connect: operation not permitted`.

## Decision Log

- Decision: Use `postgres:latest` and `dpage/pgadmin4:latest` images in Docker Compose.
  Rationale: The milestone calls for the latest PostgreSQL and pgAdmin versions with minimal maintenance overhead.
  Date/Author: 2026-01-06 / Codex
- Decision: Keep the integration test project out of the solution so `dotnet test` does not run it by default.
  Rationale: Default test runs should remain fast and not require Docker; integration tests can be run explicitly.
  Date/Author: 2026-01-06 / Codex
- Decision: Pin the Postgres server version for EF Core model comparison to 18.0.
  Rationale: Npgsql uses the configured server version to build the model; pinning avoids pending model change warnings when running migrations.
  Date/Author: 2026-01-06 / Codex

## Outcomes & Retrospective

PostgreSQL is now the persistence backend, docker compose includes PostgreSQL and pgAdmin, and an opt-in Testcontainers integration test project is in place. Tests and Docker validation remain environment-dependent and should be rerun in a full Docker-capable environment.

## Context and Orientation

Persistence currently uses EF Core with SQLite in `src/SilkHat.Infrastructure`. The API config in `src/SilkHat.Api` wires the DbContext and runs migrations on startup. Docker Compose in the repository root starts the API and UI. This milestone replaces the SQLite provider with PostgreSQL, ensuring the connection string and provider configuration are set via environment variables. The system must still run locally and via Docker Compose, and the data should be inspectable using standard PostgreSQL tooling and pgAdmin. Integration tests should live in a dedicated test project that uses Testcontainers to start PostgreSQL for tests, and those tests should not run by default with `dotnet test` unless explicitly requested.

## Plan of Work

First, inspect the EF Core DbContext configuration and migrations to see how SQLite is wired and where connection strings are sourced. Next, switch the provider to Npgsql, add the Npgsql EF Core package, and ensure migrations target PostgreSQL. Then update the API configuration to read a PostgreSQL connection string from environment variables and ensure migrations run at startup without manual intervention. Update docker-compose to add a PostgreSQL service (latest stable version) with a volume for persistence, add pgAdmin configured to connect to the PostgreSQL service, and configure the API to depend on it. Add a new integration test project that uses Testcontainers to start PostgreSQL for database integration tests, and configure it so these tests do not run as part of the default `dotnet test` invocation. Finally, update or add tests to validate the repository config persistence continues to work, run `dotnet test` for default tests, run the integration tests explicitly, and verify the Docker stack starts cleanly with PostgreSQL and pgAdmin.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    dotnet test tests/SilkHat.IntegrationTests/SilkHat.IntegrationTests.csproj
    docker compose up --build -d

## Validation and Acceptance

Run `dotnet test` and expect all tests to pass. Start Docker and verify:

- The API boots with PostgreSQL and applies migrations successfully.
- The UI loads and can access repository config/group endpoints.
- Data persists across API restarts and can be inspected via PostgreSQL tools.
- pgAdmin can connect to the PostgreSQL service and list the application database.
- Integration tests run via an explicit command and pass against a Testcontainers PostgreSQL instance.

## Idempotence and Recovery

Database migrations are idempotent; rerunning the API should apply pending migrations without data loss. If migrations fail, capture the error, adjust the migration scripts or configuration, and rerun the API. Docker Compose can be restarted without losing data because the PostgreSQL volume persists.

## Artifacts and Notes

Record any migration issues, connection string quirks, or PostgreSQL-specific behaviors encountered during the switch.

## Interfaces and Dependencies

Update EF Core configuration in `src/SilkHat.Infrastructure` to use `UseNpgsql` and add the required Npgsql EF Core package. Update API configuration in `src/SilkHat.Api` to read the PostgreSQL connection string from environment variables. Update `docker-compose.yml` to include a PostgreSQL service and wire the API to it via environment variables.

## Test Plan

Add or update tests in `tests/SilkHat.Api.Tests` and `tests/SilkHat.Tests` to validate persistence and migration behavior. Create a new test project (for example, `tests/SilkHat.IntegrationTests`) that uses Testcontainers for PostgreSQL integration tests. Ensure these integration tests are excluded from default `dotnet test` runs (for example, by setting a custom target or by using a distinct test project that is only executed when explicitly invoked). Continue to use the existing testing standards: controller tests must verify dependency calls and responses, service tests must verify edge cases, and integration tests should use real implementations without external infrastructure beyond Testcontainers.

Plan update (2026-01-06 14:46Z): Created M5e ExecPlan for PostgreSQL migration and Docker Compose refinement.
Plan update (2026-01-06 14:46Z): Added Testcontainers-based integration test project requirements and opt-in test execution rules.
Plan update (2026-01-06 14:46Z): Added pgAdmin requirement and Docker Compose wiring to the M5e plan.
Plan update (2026-01-06 14:46Z): Implemented PostgreSQL provider switch, updated migrations, docker-compose services, and added Testcontainers integration test project.
Plan update (2026-01-06 14:46Z): Attempted `dotnet test` and Docker verification; both blocked by environment permissions and recorded in Surprises & Discoveries.
Plan update (2026-01-06 14:46Z): Pinned EF Core model Postgres version to 18.0 to resolve pending model change warnings.
