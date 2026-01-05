# M5c Repository Group Loader UI Overhaul

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone replaces the existing repository config/group UI with a dedicated repository group loader component that matches the API behavior for browsing and loading repositories. After completion, users can create and edit repository groups from the available repositories list, select which solution files to load per repository, and load an entire group in one action that resets existing workspaces and recompiles the selected solutions.

## Progress

- [ ] Add a new repository group loader component that replaces the existing dashboard group/config UI.
- [ ] Enable creation of repository groups and selection of repositories from the available list, with per-repository solution selection.
- [ ] Enable editing of existing groups: add/remove repositories and enable/disable solutions.
- [ ] Update load workflow to reload group repositories by destroying existing workspaces and creating fresh compilations.
- [ ] Update or add UI tests covering rendering and key interactions in the new component.
- [ ] Run `dotnet test` and verify all non-infrastructure tests pass.
- [ ] Run `docker compose up --build` and confirm API + UI run in Docker.
- [ ] Write Milestone Commit Notes for M5c and update milestone state files.

## Surprises & Discoveries

None yet.

## Decision Log

None yet.

## Outcomes & Retrospective

To be completed at milestone end.

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
