# M8 IDE-Only Fluxor State Management (Phase 1)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this milestone, the IDE page uses Fluxor for state management while the rest of the UI remains unchanged. The IDE state is explicit and testable via Fluxor actions and reducers, and the view is broken into reusable components (solution selector, repository tree, file tabs, commit card). This enables scalable composition for future Roslyn + git dashboards without rewriting the entire UI now.

## Progress

- [x] (2026-01-08 15:33Z) Captured the IDE-only Fluxor migration scope and created this ExecPlan.
- [x] (2026-01-08 15:45Z) Added Fluxor registration, IDE state/actions/reducers/effects, and correlation-id HTTP handler.
- [x] (2026-01-08 15:45Z) Split IDE into Fluxor-driven components and rewired the IDE page.
- [x] (2026-01-08 15:45Z) Updated IDE bUnit tests to use Fluxor-aware wiring with explicit state injection.
- [x] (2026-01-08 16:50Z) Validated `dotnet test` and confirmed docker compose UI/API behavior.
- [x] (2026-01-08 16:50Z) Produced Milestone Commit Notes for M8.

## Surprises & Discoveries

- Fluxor store initialization was missing in `App.razor`; without `StoreInitializer` effects never ran.
- bUnit Fluxor components required `IActionSubscriber` plus explicit `IState<T>` registrations to render.
- MSBuild named-pipe failures (permission denied) surfaced when running tests in parallel; single-node runs avoided it.

## Decision Log

- Decision: Scope Fluxor adoption to the IDE only in Phase 1; leave other pages on existing local state.
  Rationale: Other pages are scheduled for a visual redesign, so migrating them now would create churn without benefit.
  Date/Author: 2026-01-08 15:33Z / Codex

- Decision: Add a client-side correlation-id handler that logs request/response IDs and injects the header on every API call.
  Rationale: This provides deterministic request tracing across Fluxor effects and API calls without complicating individual client methods.
  Date/Author: 2026-01-08 15:45Z / Codex

## Outcomes & Retrospective

- IDE page now uses Fluxor state with explicit solution/tree/tab slices and reusable components.
- Correlation-id propagation/logging established for UI API calls.
- IDE tests cover tree rendering, tab opening, commit card display, and whitespace-preserving file rendering.
- UI architecture docs updated to track Fluxor state/actions/effects by domain.

## Context and Orientation

The IDE view is defined in `src/SilkHat.Ui/Pages/Ide.razor` and currently owns local state for solution selection, tree items, open tabs, and diff rendering. It uses `src/SilkHat.Ui/Services/RepositoryApiClient.cs` to call API endpoints and `src/SilkHat.Ui/Models/*.cs` for DTOs. The IDE renders a repository tree via `MudTreeView`, file tabs via `MudTabs`, and a commit metadata card with a diff toggle.

Fluxor is a predictable state container for Blazor. In Fluxor, an action is a message describing a change, a reducer is a pure function that returns a new state, and an effect is an async handler that reacts to actions (typically API calls) and dispatches success/failure actions. State slices (features) live in dedicated folders and are injected into components.

This milestone introduces Fluxor only for the IDE. All other pages keep their current local state management.

## Plan of Work

Add Fluxor packages and register them in the UI startup. Create an IDE feature folder structure under `src/SilkHat.Ui/State/Ide` with three state slices:

1) `IdeSolutionsState`: available solutions, selected solution id, load status and errors.
2) `IdeTreeState`: tree nodes per solution, loading flags, and tree errors.
3) `IdeTabsState`: open file tabs per solution, active tab index per solution, diff toggle state, and per-tab commit metadata.

Define actions that mirror existing UI behavior, such as `LoadSolutions`, `SelectSolution`, `LoadTreeRoot`, `LoadTreeChildren`, `OpenFileTab`, `CloseFileTab`, `CloseAllTabs`, `ToggleDiff`, plus success/failure results for each async path. Implement reducers to update state deterministically without side effects.

Implement effects that call `RepositoryApiClient`:
1) When solutions are requested, call `GetLoadedRepositoryConfigsAsync` and derive the enabled solutions list.
2) When a solution is selected, ensure root tree nodes are loaded and ready.
3) When a file is opened, call `GetCodeFileAsync` and `GetGitFileLastChangeAsync` (includeDiff false).
4) When diff toggle is enabled, call `GetGitFileLastChangeAsync` (includeDiff true) if the diff data is not already present.

Split `Ide.razor` into components in `src/SilkHat.Ui/Components/Ide`:
1) `IdeSolutionSelector.razor` (select solutions and display errors).
2) `IdeTree.razor` (tree rendering + lazy loading; dispatch load actions).
3) `IdeTabs.razor` (open tabs, close tabs, diff checkbox, file text rendering).
4) `IdeCommitCard.razor` (MudCard that shows short SHA, date, author, author email, and diff toggle).

Replace the local state in `Ide.razor` with Fluxor state subscription and action dispatch. Keep the page layout intact, but move the commit card into its component and wire it to Fluxor.

Update `docs/architecture.md` with an IDE-focused UI Architecture section that enumerates the state slices, actions/effects, endpoints, logging, and component usage. Keep this section updated as M8 evolves.

Update tests in `tests/SilkHat.Ui.Tests/Pages/IdeTests.cs` to use the Fluxor store. For async effects, use a mock `RepositoryApiClient` registered in DI and assert that components render based on store updates. Add test helpers if needed to dispatch actions and wait for renders. Ensure tests cover:
1) Opening a file tab displays commit metadata card details.
2) Diff toggle triggers a diff fetch and updates line rendering.
3) Closing tabs updates state and hides tab content.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d

## Validation and Acceptance

- IDE view renders from Fluxor state; the page does not use local fields for solution/tree/tab/diff state.
- Opening a file tab shows the commit card with short SHA, date, author, and author email, and the diff toggle remains functional.
- `dotnet test` passes, and Docker API/UI start successfully with `docker compose up --build`.

## Idempotence and Recovery

Fluxor state changes are additive and can be safely re-run by reloading the app. If effects fail, the UI should show error messages without corrupting state. If a migration step is incomplete, revert the IDE components to local state or stub reducers to keep the UI compiling while finishing the migration.

## Artifacts and Notes

Include any relevant Fluxor action names, reducer signatures, or error logs discovered during migration as short indented snippets when updating this plan.

## Interfaces and Dependencies

Add NuGet package `Fluxor` to `src/SilkHat.Ui/SilkHat.Ui.csproj`, and register it in `src/SilkHat.Ui/Program.cs` with `services.AddFluxor(...)`.

Create state models under:
- `src/SilkHat.Ui/State/Ide/IdeSolutionsState.cs`
- `src/SilkHat.Ui/State/Ide/IdeTreeState.cs`
- `src/SilkHat.Ui/State/Ide/IdeTabsState.cs`

Create action/reducer/effect files under `src/SilkHat.Ui/State/Ide/Actions`, `src/SilkHat.Ui/State/Ide/Reducers`, and `src/SilkHat.Ui/State/Ide/Effects`. Each effect should depend on `RepositoryApiClient` and dispatch success/failure actions.

Components should live in `src/SilkHat.Ui/Components/Ide` and reference Fluxor state via `IState<T>` and `IDispatcher`.

## Test Plan

- Update `tests/SilkHat.Ui.Tests/Pages/IdeTests.cs` to dispatch Fluxor actions and assert markup changes, including commit card details and diff annotations.
- Add tests for reducer behavior if the state logic is non-trivial (e.g., handling per-solution tab state).
- Run `dotnet test` and expect all tests to pass; ensure IDE tests pass after migration.

Plan update note: Initial ExecPlan created for IDE-only Fluxor migration Phase 1 (2026-01-08 15:33Z).
