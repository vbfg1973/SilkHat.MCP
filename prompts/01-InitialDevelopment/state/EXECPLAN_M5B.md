# M5b Repository Root Discovery and Group Loads

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone enables fast discovery of repositories from a configured root path inside the API container and supports loading repositories in bulk by group. After completion, the API lists repositories quickly from `REPO_ROOT`, validates git folders, allows grouping, and only allows IDE access for loaded repositories.

## Progress

- [x] (2026-01-06 13:30Z) Add `REPO_ROOT` configuration to Docker compose and mount `/repos` volume for the API container.
- [x] (2026-01-06 13:30Z) Implement fast repository discovery under `REPO_ROOT` with git repository verification.
- [x] (2026-01-06 13:30Z) Add API endpoints to list available repositories and associate them to groups.
- [x] (2026-01-06 13:30Z) Support loading multiple repositories at once from repository groups.
- [x] (2026-01-06 13:30Z) Restrict IDE screens to loaded repositories only.
- [x] (2026-01-06 13:30Z) Add controller/service/UI tests for discovery, validation, and group loads.
- [x] (2026-01-06 13:38Z) Run `dotnet test` and verify all non-infrastructure tests pass.
- [x] (2026-01-06 13:45Z) Run `docker compose up --build` and confirm API + UI run in Docker.
- [x] (2026-01-06 13:52Z) Write Milestone Commit Notes for M5b and update milestone state files.

## Surprises & Discoveries

- Observation: Docker bind-mounting `./repos` failed until the directory existed with writable permissions.
  Evidence: `error while creating mount source path '/home/vbfg/RiderProjects/SilkHat.MCP/repos': chown ... permission denied` until `mkdir -p repos`.

## Decision Log

- Decision: Use `REPO_ROOT` and a mounted `/repos` volume to scope repository discovery.
  Rationale: Ensures predictable, fast repository listing and avoids scanning arbitrary filesystem paths.
  Date/Author: 2026-01-05 / Codex
- Decision: Validate repository paths against `REPO_ROOT` for all config create/update operations.
  Rationale: The milestone requires repositories of interest to live under the configured root and be verifiable git repositories.
  Date/Author: 2026-01-06 / Codex

## Outcomes & Retrospective

Repository discovery and group loads are implemented, with API and UI support for listing available repositories, validating git roots under `REPO_ROOT`, loading repository groups in bulk, and restricting IDE selection to loaded repositories. Tests and Docker validation are complete.

## Context and Orientation

Repository configs and groups are stored in `SilkHat.Infrastructure` with controllers in `src/SilkHat.Api/Controllers`. Repository load is orchestrated in `RepositoryLoadController` and depends on repositories being loaded in `LoadedRepositoryStore`. UI screens live in `src/SilkHat.Ui/Pages/Home.razor` and `src/SilkHat.Ui/Pages/Ide.razor`. This milestone adds discovery endpoints and enforces a loaded-only constraint in the IDE.

## Plan of Work

Introduce `REPO_ROOT` configuration and map it to `/repos` in Docker compose. Add a discovery service that enumerates directories under `REPO_ROOT` quickly and validates git repositories by checking for a `.git` directory or by running a cheap git command. Expose endpoints to list available repositories and add them to groups only if validation succeeds. Extend repository load to accept group loading, and restrict IDE access to loaded repositories. Update the UI to list available repositories and show load state. Add tests for discovery, validation, group loads, and loaded-only access.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false
    docker compose up --build -d

## Validation and Acceptance

Run tests and confirm they pass. Run Docker, then verify:
- Available repositories list is fast and limited to `REPO_ROOT`.
- Repositories can only be added to groups if they are git repositories.
- Group load triggers multiple repository loads.
- IDE screens only show or allow access to loaded repositories.

## Idempotence and Recovery

Discovery is read-only and can be re-run safely. If `REPO_ROOT` is missing, return ProblemDetails with a clear error.

## Artifacts and Notes

Capture a sample response of repository discovery and group load results.

## Interfaces and Dependencies

Add a repository discovery service under `src/SilkHat.Analysis` or `src/SilkHat.Git.Analysis` as appropriate. Use `IGitCommandRunner` for validation when necessary, but prefer quick filesystem checks.

## Test Plan

Add unit tests for discovery and git validation (temporary directories and `.git` markers). Add controller tests for the new discovery endpoints and group load behavior. Add UI tests for repository lists and load gating. Run `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false`.

Plan update (2026-01-05 16:16Z): Created M5b ExecPlan for repository discovery and group loading.
Plan update (2026-01-06 13:45Z): Recorded implementation, validation results, and Docker verification for M5b.
Plan update (2026-01-06 13:52Z): Recorded milestone commit notes completion.
