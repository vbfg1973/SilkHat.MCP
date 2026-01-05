# M1 Skeleton + Dockerized Hello World

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone establishes the baseline solution structure, a working API with a health endpoint, a Blazor WASM UI using MudBlazor with a persisted theme toggle, and Docker images that run the API and UI together. A developer can run the API and UI locally or via `docker compose up --build` and see the UI call `/api/health` successfully.

## Progress

- [x] (2026-01-05 11:52Z) Created the solution and project skeleton under `/src` and `/tests`, added projects to `SilkHat.sln`.
- [x] (2026-01-05 11:52Z) Implemented `/api/health` in `src/SilkHat.Api/Program.cs` and enabled Swagger.
- [x] (2026-01-05 11:52Z) Swapped the UI to MudBlazor with a persisted light/dark theme toggle and added a health call on the dashboard.
- [x] (2026-01-05 11:52Z) Added Dockerfiles for API/UI plus root `docker-compose.yml` and nginx proxy for `/api`.
- [x] (2026-01-05 11:52Z) Added minimal `/docs/README.md` for local and Docker run instructions.
- [x] (2026-01-05 12:00Z) Added `.dockerignore` to exclude `bin/` and `obj/` so container restores are not overwritten by host artifacts.

## Surprises & Discoveries

- Observation: The installed SDK is `9.0.112`, not .NET 10.
  Evidence: `dotnet --version` returned `9.0.112`.
- Observation: Docker publish initially failed because host `obj/` overwrote container restore outputs.
  Evidence: `NETSDK1064` missing package errors during `dotnet publish --no-restore` inside Docker.

## Decision Log

- Decision: Target `net9.0` for M1 projects so local builds/tests succeed with the installed SDK, while keeping structure ready for a future .NET 10 bump.
  Rationale: .NET 10 SDK is not available in the environment; using `net9.0` keeps the milestone shippable.
  Date/Author: 2026-01-05 / Codex
- Decision: Add `.dockerignore` to prevent host build artifacts from overwriting container restore results.
  Rationale: Prevents missing-package failures during Docker publish steps.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M1 delivers a runnable API and UI plus Docker-based deployment that can be verified with `/api/health` from the UI. Remaining work includes persistence, streaming, code analysis, git analysis, and UI expansion per later milestones.

## Context and Orientation

The solution root contains `SilkHat.sln`, `docker-compose.yml`, `/src` for application projects, `/tests` for test projects, and `/docs` for documentation. The API project lives at `src/SilkHat.Api` and the UI lives at `src/SilkHat.Ui`. The API exposes `/api/health` for smoke testing. The UI is a Blazor WASM app using MudBlazor components and a light/dark theme toggle persisted to local storage. Docker uses an nginx reverse proxy to forward `/api` requests to the API container.

## Plan of Work

Create the solution and all required projects, then replace the API template with a minimal health endpoint and Swagger. Integrate MudBlazor into the UI, add a persisted theme toggle, and display the health check status on the dashboard. Add Dockerfiles for API and UI (including nginx config for proxying `/api`) and a root `docker-compose.yml` that builds and runs both containers. Document local and Docker run commands under `/docs/README.md`.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet new sln -n SilkHat
    dotnet new classlib -n SilkHat.Core -o src/SilkHat.Core -f net9.0
    dotnet new webapi -n SilkHat.Api -o src/SilkHat.Api -f net9.0
    dotnet new blazorwasm -n SilkHat.Ui -o src/SilkHat.Ui -f net9.0
    dotnet sln SilkHat.sln add src/SilkHat.Api/SilkHat.Api.csproj src/SilkHat.Ui/SilkHat.Ui.csproj
    dotnet add src/SilkHat.Api/SilkHat.Api.csproj package Swashbuckle.AspNetCore
    dotnet add src/SilkHat.Ui/SilkHat.Ui.csproj package MudBlazor
    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` from the repo root and expect all tests to pass. Run `docker compose up --build`, then navigate to `http://localhost:5001` and confirm the UI loads and shows a green health status from `http://localhost:5000/api/health`. Swagger UI should be available at `http://localhost:5000/swagger`.

## Idempotence and Recovery

All creation steps are additive and can be re-run without harmful side effects. If Docker builds fail, retry `docker compose up --build` after verifying Docker is running and that ports `5000` and `5001` are available.

## Artifacts and Notes

Expected health response:

    GET http://localhost:5000/api/health
    OK

## Interfaces and Dependencies

The API exposes `GET /api/health` returning a 200 OK with body `OK`. The UI uses MudBlazor with a `ThemeService` that persists the theme in `localStorage` under the key `silkhat.theme`. Docker uses nginx to proxy `/api` to the `api` service on port `8080`.
