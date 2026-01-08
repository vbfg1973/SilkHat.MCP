# Add IDE Symbol Browser Popup (Named Types + Members)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

After this change, a user can open a popup in the IDE view that lists all named types defined in the currently open file and their members (fields, methods, properties, etc.). Each node is displayed as a compact MudCard inside a MudTreeView so the user can quickly scan types and their contents. This provides the foundation for future navigation commands (for example, “find callers”) while already offering a useful type outline for the active file. The feature is visible and usable in the running UI and is verified by tests.

## Progress

- [x] (2026-01-08 16:50Z) Create this ExecPlan and confirm scope/constraints.
- [x] (2026-01-08 17:24Z) Add API support for per-file symbol outline (named types + members).
- [x] (2026-01-08 17:24Z) Add UI models, API client calls, Fluxor state/actions/effects, and popup component.
- [x] (2026-01-08 17:24Z) Update UI architecture docs and add tests.
- [x] (2026-01-08 20:06Z) Validate with tests and docker compose; produce milestone commit notes.

## Surprises & Discoveries

- VSTest sockets can fail in this environment (`SocketException (13): Permission denied`), even with `dotnet test -m:1`.
  Evidence: VSTest `SocketServer.Start` failure during `dotnet test -m:1`.
  Evidence detail: `dotnet test -m:1` aborts with `System.Net.Sockets.SocketException (13): Permission denied` for `SilkHat.Tests`, `SilkHat.Api.Tests`, and `SilkHat.Ui.Tests`.
- Docker Compose cannot access the daemon socket in this environment.
  Evidence: `docker-compose up --build -d` fails with `connect: operation not permitted` on `/var/run/docker.sock`.

## Decision Log

- Decision: Use a MudDialog popup triggered from the IDE tabs header to show the file symbol outline.
  Rationale: A dialog is the most natural “popup” in MudBlazor and supports future cross-component navigation events.
  Date/Author: 2026-01-08 / Codex

- Decision: Keep the popup synchronized with the active tab when it is open.
  Rationale: Users expect the outline to describe whatever file is currently focused, and the button label implies a per-tab view.
  Date/Author: 2026-01-08 / Codex

## Outcomes & Retrospective

The IDE now exposes a symbols popup that lists named types and members for the active file, wired end-to-end from API to UI with Fluxor state and tests. Validation was completed after local test and docker permissions were restored.

## Context and Orientation

The IDE view uses Fluxor state slices under `src/SilkHat.Ui/State/Ide` and renders the tree and tabs through components in `src/SilkHat.Ui/Components/Ide`. The tab header is in `src/SilkHat.Ui/Components/Ide/IdeTabs.razor`, which already contains the “Close all tabs” button and commit card. The API currently exposes named types via `src/SilkHat.Api/Controllers/CodeNamedTypesController.cs`, but there is no endpoint to return a per-file symbol outline or member list. The code analysis layer uses Roslyn compilations stored in `CodeSolutionWorkspace` and already records named type locations and file paths.

A “symbol outline” here means a hierarchical list where top-level nodes are named types found in the current file, and child nodes are members of those types (methods, fields, properties, events, nested types). For each node we show a name plus two kind labels: SymbolKind (broad category such as NamedType, Method, Field) and RealType (more specific classification, such as Class/Record/Interface for named types or Constructor/Property for methods). We do not display namespaces in this popup.

## Plan of Work

Implement a new API endpoint to return a per-file symbol outline. The endpoint should use the repository id and solution id and take a file path query parameter. Use Roslyn compilations already stored in `CodeSolutionWorkspace` to locate the syntax tree for the file and enumerate symbols. The response should be a tree DTO with children. It must include SymbolKind and RealType for every node, with RealType reflecting the specific classification for named types (class/record/interface/enum/struct/delegate) and member methods (constructor, ordinary method, property, field, event). A stable identifier (symbol key) should also be included so the UI can later send symbol-based commands to the API. The endpoint must reject an empty path and return a helpful problem response when the repository or solution is missing.

Add UI models to represent the symbol outline nodes, add an API client method to request the outline, and add a Fluxor state slice and actions for the popup. The state should track whether the popup is open, the current file path, loading/error status, and the current tree of symbols. Effects should load the symbol outline when the popup opens and there is an active file tab, and log failures.

Add a new component for the popup. The popup should be opened via a neutral button placed in the IDE tabs header, to the left of “Close all tabs,” keeping “Close all tabs” as the right-most button. The popup content is a MudTreeView whose item template is a small MudCard showing the symbol’s name and both kinds. Each node should be clickable, and clicking should dispatch a Fluxor action so later enhancements can open a file or perform other symbol queries. For now, clicking can simply dispatch a “selected symbol” action that records the last selected symbol in state.

Update `docs/architecture.md` UI Architecture section with the new symbol outline state/actions/effects and the popup component, as required by PLANS.md. Include the API endpoint used and the logging behavior for requests.

## Concrete Steps

Work from `/home/vbfg/RiderProjects/SilkHat.MCP`.

1) Add core DTOs and API endpoint.
   - Create a DTO in `src/SilkHat.Code.Core/Dtos` for a symbol tree node (name, symbol kind, real type, symbol key, children).
   - Add a query model and validator in `src/SilkHat.Api/Models` and `src/SilkHat.Api/Validation` for the file path (required, non-empty).
   - Add a new controller in `src/SilkHat.Api/Controllers` (for example, `CodeFileSymbolsController`) with route:
       api/repositories/{id}/code/solutions/{solutionId}/files/symbols?path={uri-encoded}
     This avoids path segments and honors the existing approach for file paths.
   - Implement a service in `src/SilkHat.Code.Analysis/Services` (for example, `CodeSymbolOutlineService`) that takes a solution workspace and file path, finds the Roslyn syntax tree and symbol model, and returns a hierarchical list of symbols. Register the service in `src/SilkHat.Api/Program.cs`.

2) Add UI models and API client call.
   - Add new models under `src/SilkHat.Ui/Models` for the symbol outline response.
   - Add a `RepositoryApiClient` method to call the new endpoint and return the symbol tree.

3) Add Fluxor state/actions/effects for the popup.
   - Under `src/SilkHat.Ui/State/Ide`, add a state slice for the symbol outline popup with open/close state, loading/error, selected symbol, and tree data per solution and file path.
   - Add actions: `OpenSymbolPopup`, `CloseSymbolPopup`, `LoadFileSymbols`, `LoadFileSymbolsSuccess`, `LoadFileSymbolsFailure`, `SelectSymbolNode`.
   - Add reducers and effects that load the outline when the popup opens and a file tab is active.

4) Add the UI popup component and wire the button.
   - Create `src/SilkHat.Ui/Components/Ide/IdeSymbolPopup.razor` using MudDialog and MudTreeView with a MudCard-based item template.
   - Update `src/SilkHat.Ui/Components/Ide/IdeTabs.razor` to add a neutral button to the left of “Close all tabs” that opens the dialog. Ensure “Close all tabs” remains right-most. The popup should follow the active tab: if the popup is open and the active tab changes, reload the symbol outline for the newly active file.
   - Ensure the dialog can dispatch Fluxor actions to select a symbol.

5) Update UI architecture docs.
   - Update the UI Architecture section in `docs/architecture.md` to describe the new symbol popup state/actions/effects, API usage, and component relationships.

6) Add tests.
   - Add unit tests for the new symbol outline service in `tests/SilkHat.Tests` to ensure it returns named types and members for a known sample file.
   - Add API controller tests in `tests/SilkHat.Api.Tests` for the new endpoint (missing workspace, missing solution, valid outline).
   - Add UI tests in `tests/SilkHat.Ui.Tests` to confirm the popup button dispatches the open action and renders symbol cards for a seeded state.

## Validation and Acceptance

Run tests from the repo root and confirm they pass:

    dotnet test -m:1

Then start the system:

    REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d

Acceptance criteria:

- The IDE tab header includes a neutral button to open the symbol popup, positioned to the left of “Close all tabs” (which remains right-most).
- Opening the popup for a file shows a MudTreeView with MudCards for each named type and its members, each showing the name, SymbolKind, and RealType.
- The API serves symbol outlines for a given solution + file path, and invalid paths return a clear problem response.

## Idempotence and Recovery

The changes are additive and safe to re-run. If an API call fails, the popup should display an error message while leaving existing state untouched. If the dialog is closed, no background tasks should remain in flight. If the feature is partially implemented, keep the dialog hidden by default so the UI remains functional.

## Artifacts and Notes

Include small excerpts of successful test output and one sample response payload from the new symbol outline endpoint once it is implemented. Keep excerpts short and focused.

## Interfaces and Dependencies

Define a new DTO for symbol outline nodes in `src/SilkHat.Code.Core/Dtos`, for example:

    public sealed record SymbolOutlineNodeDto(
        string SymbolKey,
        string Name,
        string SymbolKind,
        string RealType,
        IReadOnlyList<SymbolOutlineNodeDto> Children);

Add a new service in `src/SilkHat.Code.Analysis/Services` (registered in `src/SilkHat.Api/Program.cs`) that uses Roslyn’s semantic model to enumerate symbols for a file. The service should accept a `CodeSolutionWorkspace` and a file path, and return a list of `SymbolOutlineNodeDto` rooted at the file’s named types.

Add a new API controller with the route described above. Use a query model to enforce a non-empty `path` parameter and validate it with FluentValidation.

In the UI, add models to represent the symbol outline nodes, add a new RepositoryApiClient method to fetch them, and wire Fluxor actions/effects/state to load and display the data.

## Test Plan

Add three test layers:

- Service tests under `tests/SilkHat.Tests` using the existing sample solution to assert named types and members appear for a known file.
- API tests under `tests/SilkHat.Api.Tests` that verify error handling and success responses for the new endpoint.
- UI tests under `tests/SilkHat.Ui.Tests` that seed Fluxor state and verify the popup renders MudCards with the correct name/kind text.

Run `dotnet test -m:1` from the repository root and ensure all tests pass. For docker validation, run `REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d` and confirm the IDE can open the symbol popup and render data.

Plan update note: Added the “popup follows active tab” requirement and captured implementation progress (2026-01-08 17:24Z).
Plan update note: Recorded latest test attempt and expanded socket failure evidence (2026-01-08 19:13Z).
Plan update note: Documented docker daemon permission failure during validation attempt (2026-01-08 19:20Z).
Plan update note: Marked validation complete after local test and docker confirmation (2026-01-08 20:06Z).
