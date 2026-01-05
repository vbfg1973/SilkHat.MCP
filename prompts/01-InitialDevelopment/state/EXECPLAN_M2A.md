# M2a Controller-Based API Endpoints

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone migrates the API from minimal endpoints to controller-based endpoints. After completion, all existing routes are implemented with controllers, ProblemDetails and correlation IDs remain consistent, and future endpoints will be added through controllers only.

## Progress

- [x] (2026-01-05 12:35Z) Converted health and repository CRUD endpoints from minimal APIs to controllers.
- [x] (2026-01-05 12:35Z) Preserved ProblemDetails + correlationId behavior through controller helpers.
- [x] (2026-01-05 12:35Z) Updated the API startup pipeline to use controllers and verified Swagger wiring.
- [x] (2026-01-05 12:37Z) Ran `dotnet test` (NU1900 warnings due to restricted vulnerability feed access).
- [x] (2026-01-05 12:37Z) Ran `docker compose up --build` successfully.
- [x] (2026-01-05 12:37Z) Updated milestone state and commit notes for M2a.

## Surprises & Discoveries

None so far.

## Decision Log

- Decision: Implement a shared `ApiControllerBase` for ProblemDetails with correlationId and category extensions.
  Rationale: Keeps error responses consistent across controllers without duplicating response assembly.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M2a delivers controller-based API endpoints for health, repository configs, and repository groups while preserving response contracts and correlation metadata.

## Context and Orientation

The API currently defines endpoints in `src/SilkHat.Api/Program.cs` using minimal APIs. Repository persistence uses `SilkHatDbContext` in `src/SilkHat.Infrastructure/SilkHatDbContext.cs`. DTOs are in `src/SilkHat.Core/Dtos`. The API also sets a correlation ID in middleware and uses ProblemDetails for validation and not-found cases. Controllers will live under `src/SilkHat.Api/Controllers` and will replace the minimal routes.

## Plan of Work

Create controller classes for health, repository configs, and repository groups. Move validation and mapping logic into controller actions, keeping the same routes and response shapes. Add a small helper for building ProblemDetails with correlationId and category extensions. Update `Program.cs` to add controllers and map them, removing minimal endpoint mappings. Confirm Swagger still lists all endpoints. Run tests and Docker validation, then update milestone state and commit notes.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` from the repo root and expect all tests to pass. Run `docker compose up --build` and verify that the UI still reaches the API and that `GET /api/health`, `GET /api/repositories`, and `GET /api/repository-groups` respond correctly. Swagger should list controller endpoints.

## Idempotence and Recovery

Controller conversions are code-only changes and can be re-applied safely. If a route regresses, compare the controller route attributes with the previous minimal endpoint paths and restore the route template.

## Artifacts and Notes

Expected response:

    GET http://localhost:5000/api/health
    OK

## Interfaces and Dependencies

Controllers should use `Microsoft.AspNetCore.Mvc` and be annotated with `[ApiController]`. The endpoints must preserve existing route templates under `/api` and return DTOs in `src/SilkHat.Core/Dtos`. ProblemDetails must include `correlationId` and `category` extensions.
