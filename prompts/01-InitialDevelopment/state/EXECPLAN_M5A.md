# M5a Git History Enhancements and Route Corrections

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone refines git history and co-change data to be more actionable and corrects git file routes to use the repository-relative path as the primary navigational element. After completion, git history entries include before/after line counts and categorical change kinds, co-change stats include total change counts for the target file, and file routes follow `/git/files/{path}/history` and `/git/files/{path}/cochanges` with URI-encoded paths.

## Progress

- [x] (2026-01-06 12:30Z) Update git DTOs to include change kind and line counts, plus total change counts for co-change stats.
- [x] (2026-01-06 12:30Z) Update git parsing to populate new DTO fields (line counts and change kinds) using stable git commands.
- [x] (2026-01-06 12:30Z) Adjust git file routes to `/git/files/{path}/history` and `/git/files/{path}/cochanges` with URI-encoded paths.
- [x] (2026-01-06 12:30Z) Update API surface documentation and UI client URLs accordingly.
- [x] (2026-01-06 12:30Z) Add/update tests for parsing, routing, and new DTO fields.
- [x] (2026-01-06 12:45Z) Run `dotnet test` and verify all non-infrastructure tests pass.
- [x] (2026-01-06 12:47Z) Run `docker compose up --build` and confirm API + UI run in Docker.
- [x] (2026-01-06 12:55Z) Write Milestone Commit Notes for M5a and update milestone state files.

## Surprises & Discoveries

None yet.

## Decision Log

- Decision: Keep git file routes in the form `/git/files/{path}/history` and `/git/files/{path}/cochanges` while requiring URI-encoded paths.
  Rationale: The path is the primary navigational element, and the requirement explicitly rejects alternative patterns.
  Date/Author: 2026-01-05 / Codex
- Decision: Decode URI-encoded paths inside the git files controller before passing them into git operations.
  Rationale: Clients encode path segments to keep them in a single route segment; decoding restores a usable repo-relative path for git commands.
  Date/Author: 2026-01-06 / Codex

## Outcomes & Retrospective

M5a now returns richer git history and co-change data, including categorical change kinds and line counts, and the git file routes are aligned with the path-first layout. UI models and docs reflect the new shapes, tests cover the enriched parsing logic, and Docker verification confirms API/UI startup on the mapped ports.

## Context and Orientation

Git DTOs live in `src/SilkHat.Git.Core/Dtos`, git parsing lives in `src/SilkHat.Git.Analysis/Services/GitCli.cs`, API routes live in `src/SilkHat.Api/Controllers/GitFilesController.cs`, and UI calls are in `src/SilkHat.Ui/Services/RepositoryApiClient.cs`. Changes here must keep paths normalized as `./...` and keep ProblemDetails `category=Git` for failures.

## Plan of Work

Extend `GitFileChangeDto` with line counts before/after and a categorical change kind enum. Extend `GitCoChangeStatsDto` with a total change count for the target file. Update git parsing to use `git show --numstat` (or equivalent stable output) to populate line counts and change kinds for each change entry. Adjust the git file routes to embed `{path}` before `history`/`cochanges` and ensure `RepositoryApiClient` uses URI-encoded paths. Update API surface docs and tests for routing and parsing.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false
    docker compose up --build -d

## Validation and Acceptance

Run tests and confirm they pass. Run Docker, then verify:
- `GET /api/repositories/{id}/git/files/{path}/history` returns history entries with change kinds and line counts.
- `GET /api/repositories/{id}/git/files/{path}/cochanges` returns co-change stats including total change count for the target file.
- UI calls use URI-encoded paths and still render history.

## Idempotence and Recovery

Git commands are read-only and can be re-run safely. If parsing fails, return ProblemDetails with `category=Git` and update tests to capture the format mismatch.

## Artifacts and Notes

Capture command output samples for `git show --numstat` and `git log --name-status` that demonstrate line count parsing.

## Interfaces and Dependencies

Introduce a `GitChangeKind` enum in `src/SilkHat.Git.Core/Dtos` and update `GitFileChangeDto` and `GitCoChangeStatsDto` accordingly. Keep the git CLI interface stable, but update `GitCli` internals to populate the new fields.

## Test Plan

Add unit tests in `tests/SilkHat.Tests` for the new parsing logic, including line counts and change kind handling. Update controller tests in `tests/SilkHat.Api.Tests` for the updated routes and response payloads. Update UI tests in `tests/SilkHat.Ui.Tests` to validate that history entries include the new fields when rendered or mapped. Run `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false`.

Plan update (2026-01-05 16:16Z): Created M5a ExecPlan to address git DTO enhancements and route corrections.
Plan update (2026-01-06 12:30Z): Recorded DTO, routing, parsing, and test updates completed for M5a.
Plan update (2026-01-06 12:55Z): Recorded validation, Docker verification, and milestone commit notes completion.
