# M2 Persistence for Repository Configs + Groups

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone lets users persist repository configurations and groups so they survive restarts. After completion, the API exposes CRUD endpoints for configs and groups backed by SQLite, the UI dashboard can list/create/edit both, and Docker runs with the database stored in a volume.

## Progress

- [x] (2026-01-05 12:22Z) Added EF Core SQLite persistence in `src/SilkHat.Infrastructure` with entity models and a DbContext.
- [x] (2026-01-05 12:22Z) Implemented CRUD endpoints for repository configs and groups in `src/SilkHat.Api/Program.cs` with validation and ProblemDetails.
- [x] (2026-01-05 12:22Z) Updated the UI dashboard in `src/SilkHat.Ui/Pages/Home.razor` to list/create/edit configs and groups.
- [x] (2026-01-05 12:22Z) Added Docker volume configuration for SQLite persistence and documented it in `docs/README.md`.
- [x] (2026-01-05 12:26Z) Ran `dotnet test` with passing results (NU1900 warnings due to vulnerability feed access).
- [x] (2026-01-05 12:26Z) Ran `docker compose up --build` successfully after updating the API Dockerfile restore inputs.
- [x] (2026-01-05 12:27Z) Updated milestone state files and commit notes for M2.

## Surprises & Discoveries

- Observation: Docker build initially failed because the API Dockerfile restored without referenced project files.
  Evidence: `NETSDK1004: Assets file '/src/src/SilkHat.Infrastructure/obj/project.assets.json' not found`.
- Observation: A transient Docker snapshot extraction error occurred during image export.
  Evidence: `failed to prepare extraction snapshot ... parent snapshot ... does not exist`.

## Decision Log

- Decision: Use `Database.EnsureCreated()` instead of migrations for M2.
  Rationale: Keeps the milestone lightweight while enabling persistence in SQLite; migrations can be introduced later.
  Date/Author: 2026-01-05 / Codex
- Decision: Store repository root paths as fully qualified paths via `Path.GetFullPath`.
  Rationale: Matches the requirement for fully qualified repo root paths and normalizes input early.
  Date/Author: 2026-01-05 / Codex
- Decision: Copy referenced project `.csproj` files into the API Docker build before restore.
  Rationale: Prevents missing-asset failures when the API references `SilkHat.Core` and `SilkHat.Infrastructure`.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M2 provides persistent repository configs and groups via SQLite, with API CRUD endpoints and a UI dashboard to manage them. Docker now persists the database via a named volume.

## Context and Orientation

The solution root includes `SilkHat.sln`, the API in `src/SilkHat.Api`, and persistence in `src/SilkHat.Infrastructure`. The API uses minimal endpoints and now references `SilkHatDbContext` from `src/SilkHat.Infrastructure/SilkHatDbContext.cs`. The UI is a Blazor WASM app in `src/SilkHat.Ui` with its dashboard defined in `src/SilkHat.Ui/Pages/Home.razor`. Docker is configured via `docker-compose.yml` at the root.

Repository configs represent a single repository root path plus metadata, and repository groups are a simple collection for organization. Persistence uses SQLite with a database file on disk; in Docker it is stored under `/data/silkhat.db` via a volume.

## Plan of Work

Add EF Core packages and create entity models in `src/SilkHat.Infrastructure/Entities` plus a `SilkHatDbContext` that manages timestamps. Update `src/SilkHat.Api/Program.cs` to register the DbContext, ensure the database exists, and implement the CRUD endpoints for both repository configs and groups using DTOs in `src/SilkHat.Core/Dtos`. Update the UI dashboard to call these endpoints and allow listing, creating, and editing both entities. Finally, update Docker configuration to persist the SQLite file in a volume and confirm everything passes tests and runs in Docker.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet add src/SilkHat.Infrastructure/SilkHat.Infrastructure.csproj package Microsoft.EntityFrameworkCore
    dotnet add src/SilkHat.Infrastructure/SilkHat.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
    dotnet add src/SilkHat.Api/SilkHat.Api.csproj reference src/SilkHat.Infrastructure/SilkHat.Infrastructure.csproj src/SilkHat.Core/SilkHat.Core.csproj
    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` from the repo root and expect all tests to pass. Run `docker compose up --build`, then navigate to `http://localhost:5001` and confirm the UI lists repository groups/configs and can create/edit entries. Verify that the API responds on `http://localhost:5000/api/repositories` and `http://localhost:5000/api/repository-groups`, and that restarting Docker preserves the data due to the volume.

## Idempotence and Recovery

All changes are additive. If the database file is corrupted, removing the volume and rerunning `docker compose up --build` will recreate it. If a connection string is misconfigured, verify the `ConnectionStrings__SilkHat` environment variable in `docker-compose.yml` and `src/SilkHat.Api/appsettings.json`.

## Artifacts and Notes

Example successful API response (list repositories):

    GET http://localhost:5000/api/repositories
    200 OK
    [
      {
        "id": "...",
        "name": "MyRepo",
        "rootPath": "/home/user/src/MyRepo",
        "description": null,
        "groupId": null,
        "createdUtc": "2026-01-05T12:00:00Z",
        "updatedUtc": "2026-01-05T12:00:00Z"
      }
    ]

## Interfaces and Dependencies

SQLite persistence is implemented with `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Sqlite` in `src/SilkHat.Infrastructure`. The DbContext is `SilkHat.Infrastructure.SilkHatDbContext` with `DbSet<RepositoryConfig>` and `DbSet<RepositoryGroup>`. API DTOs are in `src/SilkHat.Core/Dtos`, and API endpoints are defined in `src/SilkHat.Api/Program.cs` to match `/api/repositories` and `/api/repository-groups`.

Revision Note: Replaced the M1 ExecPlan with the M2 plan to track persistence and UI CRUD work for milestone 2.
Revision Note: Updated progress and discoveries after running tests and Docker validation, and recorded the Dockerfile fix.
Revision Note: Marked milestone state updates complete and summarized outcomes after finishing M2.
