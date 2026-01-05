# M3 Command/Channel Streaming + Repository Load Orchestration

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone introduces per-repository command processing with streaming progress events over NDJSON. After completion, the API provides `/api/repositories/{id}/load` and `/api/repositories/{id}/unload` using controller endpoints, and the UI shows live load progress in the dashboard.

## Progress

- [x] (2026-01-05 12:45Z) Added repo command abstractions, load command, and in-memory loaded repository store using channels.
- [x] (2026-01-05 12:46Z) Implemented streaming load/unload controller endpoints with NDJSON output.
- [x] (2026-01-05 12:46Z) Updated UI to display repository load progress stream.
- [x] (2026-01-05 12:49Z) Ran `dotnet test` (NU1900 warnings due to restricted vulnerability feed access).
- [x] (2026-01-05 12:49Z) Ran `docker compose up --build` successfully after Dockerfile and UI fixes.
- [x] (2026-01-05 12:49Z) Updated milestone state and commit notes.

## Surprises & Discoveries

- Observation: NDJSON streaming requires manual newline flushing to update the UI in real time.
  Evidence: Controller writes JSON per line and flushes the response stream after each event.
- Observation: Docker build failed until the UI MudList types were specified and the API Dockerfile restored the Analysis project.
  Evidence: `RZ10001` for `MudList`/`MudListItem` and `Skipping project "/src/src/SilkHat.Analysis/SilkHat.Analysis.csproj"`.

## Decision Log

- Decision: Implement a simple `LoadedRepositoryStore` in memory and serialize load operations per repository with `SemaphoreSlim`.
  Rationale: Matches the milestone requirement while keeping state isolated and easy to replace with a real workspace later.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M3 delivers command streaming with NDJSON load events and a UI view that renders live progress.

## Context and Orientation

Repository configs and groups are stored in SQLite via `SilkHatDbContext` (`src/SilkHat.Infrastructure/SilkHatDbContext.cs`). The API uses controllers in `src/SilkHat.Api/Controllers`. M3 introduces repository commands and streaming in `src/SilkHat.Analysis`, and the UI dashboard in `src/SilkHat.Ui/Pages/Home.razor` is updated to show load progress.

## Plan of Work

Add command abstractions (`IRepoCommand`, `IRepoCommandProcessor`) and a `LoadedRepositoryStore` in `src/SilkHat.Analysis`. Implement a `LoadRepositoryCommand` that yields progress and completed events. Add API controller endpoints that stream NDJSON for load and return a response for unload. Update the UI client to read NDJSON streaming responses and render progress events. Validate via tests and Docker, then update milestone state and commit notes.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` from the repo root and expect all tests to pass. Run `docker compose up --build`, open the UI at `http://localhost:5001`, and click load on a repository config to see streaming progress events. Verify `POST /api/repositories/{id}/load` responds with `application/x-ndjson` and one JSON object per line.

## Idempotence and Recovery

All changes are additive and can be re-run safely. If streaming fails, confirm the content type and that each event is newline-delimited JSON. If the loaded repository state becomes stale, call `/api/repositories/{id}/unload` to remove it from memory.

## Artifacts and Notes

Example NDJSON response:

    {"kind":"progress","stage":"validate","message":"Validating repository configuration.","percent":10}
    {"kind":"progress","stage":"workspace","message":"Preparing repository workspace placeholder.","percent":60}
    {"kind":"completed","stage":"load","message":"Repository loaded.","percent":100,"summary":{"message":"Loaded placeholder workspace."}}

## Interfaces and Dependencies

`SilkHat.Analysis.Abstractions.IRepoCommand` executes per repository and returns `IAsyncEnumerable<RepoEventDto>` from `src/SilkHat.Core/Dtos/RepoEvents.cs`. `LoadedRepositoryStore` coordinates in-memory loaded repositories. API controllers expose streaming endpoints and the UI consumes NDJSON via `HttpClient` streaming.
