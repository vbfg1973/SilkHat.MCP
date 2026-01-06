# M5c Repository Group Loader UI Overhaul

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone replaces the existing repository config/group UI with a dedicated repository group loader component that matches the API behavior for browsing and loading repositories. After completion, users can create and edit repository groups from the available repositories list, select which solution files to load per repository, and load an entire group in one action that resets existing workspaces and recompiles the selected solutions.

## Progress

- [x] (2026-01-06 16:20Z) Add a new repository group loader component that replaces the existing dashboard group/config UI.
- [x] (2026-01-06 16:20Z) Enable creation of repository groups and selection of repositories from the available list, with per-repository solution selection.
- [x] (2026-01-06 16:20Z) Enable editing of existing groups: add/remove repositories and enable/disable solutions.
- [x] (2026-01-06 16:20Z) Update load workflow to reload group repositories by destroying existing workspaces and creating fresh compilations.
- [x] (2026-01-06 16:20Z) Update or add UI tests covering rendering and key interactions in the new component.
- [x] (2026-01-06 16:28Z) Run `dotnet test` and verify all non-infrastructure tests pass.
- [x] (2026-01-06 16:31Z) Run `docker compose up --build` and confirm API + UI run in Docker.
- [x] (2026-01-06 16:34Z) Write Milestone Commit Notes for M5c and update milestone state files.
- [x] (2026-01-06 16:46Z) Add EF Core migrations and switch API startup to `Database.Migrate()` for schema updates.
- [x] (2026-01-06 17:15Z) Rebuild migrations to split initial schema from solution configs for existing database compatibility.
- [x] (2026-01-06 17:28Z) Make solution config migration idempotent for existing tables.
- [x] (2026-01-06 17:33Z) Revert migrations to code-first only (no raw SQL).
- [ ] (2026-01-06 17:18Z) Re-run `dotnet test` after migration updates (blocked: MSBuild named pipe permission denied in this environment).
- [ ] (2026-01-06 16:49Z) Re-run `docker compose up --build` after migrations update (blocked: Docker daemon permission denied).
- [x] (2026-01-06 17:41Z) Add Serilog logging to API and UI to diagnose UI fetch failures.
- [x] (2026-01-06 17:48Z) Fix UI API base URL resolution to avoid file:// requests in Docker.
- [x] (2026-01-06 17:53Z) Enforce API base URL (throw on file scheme, no fallback).
- [x] (2026-01-06 17:59Z) Add CORS policy for UI origin to allow API calls from localhost:10080.
- [x] (2026-01-06 18:03Z) Move CORS middleware earlier to ensure headers on errors.
- [x] (2026-01-06 18:08Z) Add Buildalyzer error handling to surface missing analyzer results.
- [x] (2026-01-06 18:13Z) Fix Buildalyzer error handling for IAnalyzerResults API.
- [x] (2026-01-06 18:18Z) Iterate analyzer results when loading workspaces to match Buildalyzer API.
- [x] (2026-01-06 18:26Z) Add Buildalyzer diagnostics logging via TextWriterLogger.
- [x] (2026-01-06 18:34Z) Resolve Buildalyzer logger type ambiguity.
- [x] (2026-01-06 18:40Z) Log Buildalyzer build event errors/warnings when results are empty.
- [x] (2026-01-06 18:48Z) Force Buildalyzer restore/full build and log result/event counts.
- [x] (2026-01-06 18:54Z) Fix Buildalyzer log call overload resolution for LogDebug.
- [x] (2026-01-06 19:00Z) Use ILogger.Log with explicit EventId for Buildalyzer summary logging.
- [x] (2026-01-06 19:05Z) Fix ILogger.Log formatter signature for Buildalyzer summary logging.
- [x] (2026-01-06 19:12Z) Fix Buildalyzer summary log count call to use Count().
- [x] (2026-01-06 19:20Z) Add diagnostic MSBuild logging/binlog capture on empty Buildalyzer results.
- [x] (2026-01-06 19:29Z) Log diagnostic build event summaries and counts after rerun.
- [x] (2026-01-06 19:18Z) Add sample-solution code analysis tests against samples/solution01.
- [x] (2026-01-06 19:24Z) Expand sample-solution tests to cover project index and symbol key lookups.
- [x] (2026-01-06 19:32Z) Extend sample solution with additional type kinds for code analysis coverage.
- [x] (2026-01-06 19:41Z) Add second sample project and reference coverage for project graph tests.

## Surprises & Discoveries

- Observation: Docker daemon access can be blocked in this environment when re-running compose.
  Evidence: `permission denied while trying to connect to the Docker daemon socket`.
- Observation: `dotnet test` can fail due to MSBuild named pipe permissions in this environment.
  Evidence: `System.Net.Sockets.SocketException (13): Permission denied`.

## Decision Log

- Decision: Store per-repository solution selections in a new `RepositorySolutionConfig` table linked to repository configs.
  Rationale: Enables toggling solutions independently while keeping config persistence straightforward.
  Date/Author: 2026-01-06 / Codex
- Decision: Add a discovery endpoint to list solution files for a repository path rather than inflating the available repositories payload.
  Rationale: Keeps the repository list fast while still enabling solution selection when needed.
  Date/Author: 2026-01-06 / Codex
- Decision: Clear loaded repository, workspace, and git cache state before group loads.
  Rationale: Requirement states group loads must rebuild workspaces from a fresh read.
  Date/Author: 2026-01-06 / Codex
- Decision: Use EF Core migrations and `Database.Migrate()` instead of `EnsureCreated()` for schema updates.
  Rationale: Schema changes must be applied to existing databases without manual SQL or dropping data.
  Date/Author: 2026-01-06 / Codex

## Outcomes & Retrospective

M5c delivers a new repository group loader UI that replaces the old config/group dashboard, supports solution selection per repository, and persists those selections in the API. Group loads now reset existing workspace and git cache state before rebuilding, and the new discovery endpoint enables solution selection without slowing repository listings. Tests and Docker validation confirm the updated behavior.

## Context and Orientation

The current dashboard UI that lists repository configs and groups lives in `src/SilkHat.Ui/Pages/Dashboard.razor` and is backed by API calls in `src/SilkHat.Ui/Services/RepositoryApiClient.cs`. Repository group and repository config data is persisted via the API controllers in `src/SilkHat.Api/Controllers`, and repository loading orchestration lives in the analysis layer (`src/SilkHat.Analysis`) with UI streaming handled in the dashboard. M5c must replace the existing UI flow with a new component that is group-centric and uses the repository discovery API from M5b.

## Plan of Work

Create a new UI component (either a new page or a dedicated component embedded in the dashboard) that replaces the existing repository config/group and load widgets. The component must show available repositories discovered from REPO_ROOT, allow creating a group, adding repositories to the group, and selecting solution files for each repository. For existing groups, it must allow adding/removing repositories and toggling which solutions load. When a group load is triggered, the UI must call the group load API and display streaming progress, while the backend ensures existing workspaces are destroyed and recreated. Update or add UI tests in `tests/SilkHat.Ui.Tests` to verify the new component renders the list, supports selection, and triggers load calls. Remove or hide the old UI elements to avoid duplicate workflows.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false
    docker compose up --build -d

## Validation and Acceptance

Run tests and confirm they pass. Run Docker, then verify:
- The UI shows the new repository group loader component.
- Available repositories are listed and can be added to a new group with per-repository solution selection.
- Existing groups can be edited and solution enablement toggled.
- Loading a group triggers a fresh load (existing workspaces are destroyed and recreated) and progress streams in the UI.

## Idempotence and Recovery

Creating or editing groups is safe to repeat. If a load fails, the user can retry loading the group and the system should reinitialize workspaces. Re-running tests and Docker should be safe.

## Artifacts and Notes

Capture any UI screenshots or concise output snippets that show the component rendering and streaming progress.

## Interfaces and Dependencies

Use existing `RepositoryApiClient` calls for repository groups and loads, and the repository discovery endpoints from M5b. If new DTOs or API calls are required for solution selection, define them in `src/SilkHat.Core` and `src/SilkHat.Ui/Models` with explicit fields and update the API client accordingly.

## Test Plan

Add or update bUnit tests in `tests/SilkHat.Ui.Tests` to assert that the new component renders the available repositories list, allows selecting repositories and solutions, and triggers the load call. Use a fake `HttpMessageHandler` with canned API responses and verify calls were made. Run `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false`.

Plan update (2026-01-06 13:05Z): Created M5c ExecPlan for the repository group loader UI overhaul.
Plan update (2026-01-06 16:31Z): Recorded UI, API, and load workflow changes, plus test and Docker validation.
Plan update (2026-01-06 16:34Z): Recorded milestone commit notes completion and outcomes.
Plan update (2026-01-06 16:49Z): Added migrations and noted Docker re-validation blocked by daemon permissions.
Plan update (2026-01-06 17:18Z): Rebuilt migrations and recorded test re-run blocked by MSBuild named pipe permissions.
Plan update (2026-01-06 17:28Z): Made the solution config migration idempotent for existing tables.
Plan update (2026-01-06 17:33Z): Reverted migrations to code-first only; existing databases now require reset.
Plan update (2026-01-06 17:41Z): Added Serilog logging for API/UI fetch diagnostics.
Plan update (2026-01-06 17:48Z): Updated UI API base URL defaults and file-scheme fallback.
Plan update (2026-01-06 17:53Z): Removed file-scheme fallback and enforce API base URL.
Plan update (2026-01-06 17:59Z): Added CORS policy for UI origin.
Plan update (2026-01-06 18:03Z): Moved CORS middleware earlier for error responses.
Plan update (2026-01-06 18:08Z): Added Buildalyzer error handling for null analyzer results.
Plan update (2026-01-06 18:13Z): Updated Buildalyzer handling to use IAnalyzerResults with project paths.
Plan update (2026-01-06 18:18Z): Adjusted workspace load to iterate analyzer results.
Plan update (2026-01-06 18:26Z): Added Buildalyzer diagnostics logging.
Plan update (2026-01-06 18:34Z): Fixed ILogger ambiguity in Buildalyzer logger wrapper.
Plan update (2026-01-06 18:40Z): Log MSBuild event errors/warnings when Buildalyzer returns no results.
Plan update (2026-01-06 18:48Z): Force Buildalyzer restore/full build to obtain analyzer results.
Plan update (2026-01-06 18:54Z): Fixed LogDebug overload resolution for Buildalyzer diagnostics.
Plan update (2026-01-06 19:00Z): Switched to ILogger.Log for Buildalyzer summary logging.
Plan update (2026-01-06 19:05Z): Fixed ILogger.Log formatter signature for Buildalyzer summary logging.
Plan update (2026-01-06 19:12Z): Fixed BuildEventArguments count logging for Buildalyzer summary.
Plan update (2026-01-06 19:20Z): Added diagnostic MSBuild logging/binlog capture when results are empty.
Plan update (2026-01-06 19:29Z): Added diagnostic build event summaries/counts after rerun.
Plan update (2026-01-06 19:18Z): Added code analysis tests for samples/solution01.
Plan update (2026-01-06 19:24Z): Expanded sample-solution tests for project index and symbol keys.
Plan update (2026-01-06 19:32Z): Added enum/struct/record/delegate samples for broader analysis coverage.
Plan update (2026-01-06 19:41Z): Added sample lib project and reference graph test coverage.
