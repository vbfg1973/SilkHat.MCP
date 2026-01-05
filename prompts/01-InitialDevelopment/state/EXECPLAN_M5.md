# M5 Git via Process: Tree Listing + File History + Co-change

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone enables repository git insights through the API, letting users browse a repository tree, inspect file history, and view co-change statistics without using a managed git library. After completion, the API exposes git-derived data for loaded repositories and the UI can browse the tree and view file history.

## Progress

- [x] (2026-01-05 16:09Z) Implemented git command runner and parsing logic for tree listing, file history, and co-change stats using git CLI process execution.
- [x] (2026-01-05 16:09Z) Added caching per LoadedRepository for path metadata and commit-to-files mapping, with invalidation on unload.
- [x] (2026-01-05 16:09Z) Added API endpoints for git tree, file history, and co-change stats with ProblemDetails + correlationId on failures.
- [x] (2026-01-05 16:09Z) Updated UI to show git tree and file history panel in the IDE-like view.
- [x] (2026-01-05 16:09Z) Added controller and service tests covering dependency calls, response correctness, and edge cases.
- [x] (2026-01-05 16:09Z) Ran `dotnet test` and verified all non-infrastructure tests pass.
- [x] (2026-01-05 16:09Z) Ran `docker compose up --build` and confirmed API + UI run in Docker.
- [x] (2026-01-05 16:09Z) Wrote Milestone Commit Notes for M5 and updated milestone state files.

## Surprises & Discoveries

- Observation: Catch-all route parameters cannot appear before trailing segments in ASP.NET Core.
  Evidence: Analyzer warning ASP0017 when using `/git/files/{*path}/history`.

## Decision Log

- Decision: Use git CLI via `Process` and parse stable output formats (`git log --name-status --date=iso-strict`, `git show --numstat`, `git ls-tree -r --name-only HEAD`) for all git queries.
  Rationale: Avoid LibGit2Sharp as required and keep parsing consistent.
  Date/Author: 2026-01-05 / Codex
- Decision: Adjust git file history/co-change routes to `/git/files/history/{*path}` and `/git/files/cochanges/{*path}` to satisfy routing constraints.
  Rationale: Catch-all parameters must be the final route segment in ASP.NET Core.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M5 delivered git tree, history, and co-change endpoints backed by process execution, added caching and tests, and surfaced git data in the IDE UI. Routes were adjusted to satisfy catch-all constraints while maintaining functionality. Docker and test validation passed.

## Context and Orientation

Git analysis belongs in `src/SilkHat.Git.Analysis`, and DTOs live in `src/SilkHat.Git.Core`. Controllers live under `src/SilkHat.Api/Controllers`, and the UI tree/history panel lives in `src/SilkHat.Ui/Pages/Ide.razor`. Loaded repository runtime state is managed by `LoadedRepositoryStore`, which must cache git-derived metadata and clear it on reload. The existing API already provides code analysis endpoints; this milestone adds git endpoints under `/api/repositories/{id}/git/...`.

## Plan of Work

Implement a process-based git command runner with explicit working directory set to the repository root. Create a `GitCli` service in `src/SilkHat.Git.Analysis` with methods to list the tree, fetch file history, and compute co-change stats using stable git command output. Add caches to `LoadedRepository` or a git-specific store keyed by repository id, invalidated on unload/reload. Expose controller endpoints per `prompts/01-InitialDevelopment/08_API_SURFACE.md` and return DTOs with normalized repo-relative paths (`./path/from/root`). Update the IDE-like UI to show a tree view and a right-hand file history panel populated via the git endpoints. Add unit and integration tests for parsing, cache behavior, and controller responses, with mocked dependencies verified. Run tests and Docker to validate.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false
    docker compose up --build -d

## Validation and Acceptance

Run tests and confirm they pass. Run Docker, then verify:
- `GET /api/repositories/{id}/git/tree` returns filtered tree entries.
- `GET /api/repositories/{id}/git/files/{*path}/history` returns commit history for the file.
- `GET /api/repositories/{id}/git/files/{*path}/cochanges` returns co-change stats.
- UI tree renders file entries and the history panel shows entries for the selected file.

All paths returned must be normalized repo-relative (`./...`) and error responses must include `correlationId` and `category=Git`.

## Idempotence and Recovery

Git commands are read-only and can be re-run safely. If git is unavailable, return ProblemDetails with `category=Git`. If caches become stale, reload the repository to rebuild them.

## Artifacts and Notes

Capture concise command output samples and parsing evidence in this plan as work progresses.

## Interfaces and Dependencies

Define `IGitCommandRunner` in `src/SilkHat.Git.Analysis` with `ExecuteAsync(repoRoot, args[], cancellationToken)` returning stdout, stderr, and exit code. Define `GitCli` with methods `ListTree`, `FileHistory`, and `CoChangeStats`, each returning DTOs in `src/SilkHat.Git.Core`. Prefer stable git flags and parse outputs with explicit tests.

## Test Plan

Add unit tests in `tests/SilkHat.Tests` for git command parsing and cache behavior (tree listing, history parsing, co-change parsing), using representative command output fixtures. Add controller tests in `tests/SilkHat.Api.Tests` that verify dependency calls and response payloads for each git endpoint, including error cases (git unavailable, repo not loaded). Add UI component tests in `tests/SilkHat.Ui.Tests` to render the IDE tree/history panel with mocked git API responses. Run `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` and ensure all non-infrastructure tests pass.

Plan update (2026-01-05 15:39Z): Added a Test Plan section per updated ExecPlan requirements.
Plan update (2026-01-05 16:09Z): Recorded implementation, tests, Docker verification, and route adjustment.
