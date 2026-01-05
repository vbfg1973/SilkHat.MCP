# M4b UI Hosting Fixes and Component Testing

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone fixes UI startup issues and establishes a repeatable component testing standard for the Blazor UI. After completion, the UI loads correctly in Docker, key components are covered by bUnit tests, and future component work follows the same testing expectations.

## Progress

- [x] (2026-01-05 14:58Z) Fixed the MudThemeProvider layout error so the UI renders without exceptions.
- [x] (2026-01-05 14:58Z) Added a UI test project with bUnit and initial component tests for existing pages/layouts.
- [x] (2026-01-05 14:58Z) Documented the UI component testing standard and added it to the definition of done.
- [x] (2026-01-05 15:04Z) Ran `dotnet test` and verified all non-infrastructure tests pass.
- [x] (2026-01-05 15:04Z) Ran `docker compose up --build` and confirmed API + UI run in Docker with the updated ports.
- [x] (2026-01-05 15:04Z) Wrote Milestone Commit Notes for M4b and updated milestone state files.

## Surprises & Discoveries

- Observation: MudBlazor components required a `MudPopoverProvider` to render select menus without throwing.
  Evidence: `Missing <MudPopoverProvider />` exception during bUnit rendering of the dashboard.
- Observation: `MudSelectItem` generic type mismatch surfaced when binding `Guid?` values.
  Evidence: `Unable to cast object of type 'MudSelect<Guid?>' to 'MudSelect<Guid>'` during component tests.

## Decision Log

- Decision: Use bUnit for UI component tests with fake HttpClient handlers and JSInterop stubs.
  Rationale: bUnit provides component rendering, dependency injection, and JS runtime stubs without requiring a browser.
  Date/Author: 2026-01-05 / Codex
- Decision: Add MudPopoverProvider to the main layout and match MudSelectItem generics to nullable GUIDs.
  Rationale: Prevents runtime errors in the UI and aligns tests with production rendering.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M4b resolved the MudThemeProvider rendering error, added missing MudPopoverProvider support, and introduced bUnit-based UI component testing with initial coverage. The UI now renders cleanly in Docker, and a documented testing standard applies to future component work.

## Context and Orientation

The UI is a Blazor WebAssembly app under `src/SilkHat.Ui`, with layout in `src/SilkHat.Ui/Layout/MainLayout.razor` and pages in `src/SilkHat.Ui/Pages`. MudBlazor provides the UI components. API endpoints are consumed via `RepositoryApiClient`, which uses `HttpClient`. UI tests will live under `tests/SilkHat.Ui.Tests`.

## Plan of Work

Update the layout to remove invalid MudThemeProvider child content usage so the component renders. Add a new test project using bUnit, register MudBlazor services and JSInterop stubs, and provide fake HttpClient responses to render existing pages. Document the UI testing standard and add the requirement to the definition of done. Validate with `dotnet test` and Docker.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false
    docker compose up --build -d

## Validation and Acceptance

Run tests and confirm they pass. Run Docker, then verify:
- `http://localhost:18080/api/health` returns `OK`.
- `http://localhost:10080/` serves the SPA HTML.

UI component tests should render the layout and dashboard with mocked services and validate the presence of expected content.

## Idempotence and Recovery

UI fixes and tests are additive and can be re-run safely. If Docker builds fail, rerun after stopping containers with `docker compose down`.

## Artifacts and Notes

Capture concise test output summaries and relevant diffs.

## Interfaces and Dependencies

Use MudBlazor services (`AddMudServices`) and bUnit for UI component tests. Inject `RepositoryApiClient` with a fake `HttpClient` and use bUnit JSInterop for `ThemeService`.

Plan update (2026-01-05 15:04Z): Recorded completed UI fixes, testing additions, and verification results after running tests and Docker.
