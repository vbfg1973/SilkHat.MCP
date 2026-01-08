# Architecture

SilkHat analyzes .NET repositories by loading repository groups, parsing solutions and projects, and exposing analysis results over a REST API for the UI.

## Major Components

- API (`src/SilkHat.Api`): ASP.NET Core controllers, ProblemDetails responses, and CORS for the UI.
- Analysis runtime (`src/SilkHat.Analysis`): repository load orchestration, command processing, and runtime state.
- Code analysis (`src/SilkHat.Code.Analysis`): solution parsing, workspace construction, code tree entries, and code file reads.
- Git analysis (`src/SilkHat.Git.Analysis`): git CLI integration with caches for history and tree metadata.
- Infrastructure (`src/SilkHat.Infrastructure`): EF Core persistence with PostgreSQL.
- UI (`src/SilkHat.Ui`): Blazor WASM IDE view, repository group loader, and solution-scoped navigation.

## Runtime Flow

1. Repository groups are configured and loaded via API endpoints.
2. The analysis layer builds a `CodeRepositoryWorkspace` per loaded repository and stores it in memory.
3. Code endpoints are solution-scoped and query the workspace for projects, namespaces, types, and tree entries.
4. File contents are read directly from disk using repository roots and tree entries for fast access.
5. The UI requests tree nodes lazily and opens file tabs when a file node is selected.

## Isolation Rules

- Each loaded repository is isolated; a `LoadedRepository` does not reference other repositories.
- Paths returned by APIs are normalized to repo-relative paths using `./` prefixes and forward slashes.

## Command Streaming

Repository load operations use channels and NDJSON streaming. The API exposes a load endpoint that emits progress and data events to the UI while work proceeds.

## UI Architecture (Fluxor)

The IDE view uses Fluxor for state management. Other UI pages remain on local component state until their redesign.

### Code (Roslyn) Domain

State:
- Solutions: available solutions, selected solution id (`IdeSolutionsState`).
- Tree: per-solution tree roots and lazy children (`IdeTreeState`).
- Tabs: per-solution open files, active tab index, diff toggle state, commit metadata (`IdeTabsState`).
- Symbols: per-solution symbol popup state, active file, and symbol outline tree (`IdeSymbolsState`).

Actions and effects:
- Load solutions, select solution, load tree root/children, open file tab, close tabs, toggle diff.
- Effects call API endpoints: `/api/repositories/loaded`, `/api/repositories/{id}/code/solutions/{solutionId}/tree`, `/api/repositories/{id}/code/solutions/{solutionId}/files`.
- Symbol popup effects call `/api/repositories/{id}/code/solutions/{solutionId}/files/symbols?path=...` and reload when the active tab changes while the popup is open.

Components:
- `IdeSolutionSelector` uses solutions state and dispatches selection actions.
- `IdeTree` uses solutions + tree state and dispatches tree load and open-file actions.
- `IdeTabs` uses tabs state and dispatches close/toggle actions.
- `IdeSymbolPopup` renders the named type/member outline for the active file and dispatches symbol selection.

### Git Domain

State:
- Commit metadata and diff lines stored on each open file tab (`IdeTabsState`).

Actions and effects:
- Toggle diff triggers `/api/repositories/{id}/git/files/{path}/last-change?includeDiff=true`.
- Initial tab open triggers `/api/repositories/{id}/git/files/{path}/last-change?includeDiff=false`.

Components:
- `IdeCommitCard` renders short SHA, date (UTC), author, author email, and diff toggle.
- `IdeTabs` renders annotated lines based on diff state.

### Logging and Correlation

Client-side logging:
- Fluxor action dispatch is logged at Debug via `FluxorLoggingMiddleware`.
- Effects log Debug for start/end, Warning for recoverable failures, Error for terminal failures.

Correlation:
- All UI API calls include `X-Correlation-Id` (GUID) and log both the local id and response header value.
- API middleware echoes or creates `X-Correlation-Id` and decorates server logs with it.

## Documentation Maintenance

Whenever UI state, actions, effects, or components change, update the UI Architecture section above to reflect the current state structure, API endpoints, logging, and component usage.
