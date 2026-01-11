# Architecture

SilkHat analyzes .NET repositories by loading repository groups, parsing solutions and projects, and exposing analysis results over a REST API for the UI.

## Major Components

- API (`src/SilkHat.Api`): ASP.NET Core controllers, ProblemDetails responses, CORS for the UI, HybridCache for read-mostly endpoints, indexing status endpoint, and thin orchestration over services.
- Analysis runtime (`src/SilkHat.Analysis`): repository load orchestration, command processing, runtime state, and indexing job status storage (`IIndexingStatusStore`).
- Graph index (`src/SilkHat.Code.Analysis`): QuikGraph-backed `GraphStore` + `GraphQueryService` containing solutions, projects, folders/files, types/members/parameters, locations, and typed edges for relationships/metrics.
- Code analysis (`src/SilkHat.Code.Analysis`): solution parsing, workspace construction, graph population, and code file reads.
- Git analysis (`src/SilkHat.Git.Analysis`): git CLI integration with caches for history and tree metadata.
- Infrastructure (`src/SilkHat.Infrastructure`): EF Core persistence with PostgreSQL.
- UI (`src/SilkHat.Ui`): Blazor WASM IDE view, repository group loader, solution-scoped navigation, status dialog with polling, tree/annotations backed by graph data.

## Runtime Flow

1. Repository groups are configured and loaded via API endpoints.
2. The analysis layer builds a `CodeRepositoryWorkspace` per loaded repository, populating the QuikGraph index (solutions → projects → folders/files → types/members/parameters with locations/types) and recording per-job status in `IIndexingStatusStore`.
3. The status API exposes per-job state; the UI polls once per second while a solution load is in-flight and stops when all jobs complete/failed or the dialog closes.
4. Code endpoints are solution-scoped and query the graph for projects, namespaces, types/members, metrics, and tree entries (no outline fallback).
5. File contents are read directly from disk using repository roots and tree entries for fast access; the UI requests tree nodes lazily and opens file tabs when a file node is selected.

## Code Tree Metrics (Annotations/Filters)

- Annotation/filter metrics are computed during solution load and attached to graph nodes (e.g., complexity metrics and git aggregates on files/types/members via typed edges).
- Git-derived metrics are computed once per repository (single `git log --numstat` pass) and attached to the graph; author counts roll up as **distinct** authors across folders/projects (union of file-level authors, not a sum of counts).
- Complexity metrics are computed via Roslyn strategies and attached to methods/types; roll-ups for folders/projects are derived from graph traversal.
- Metrics and graph state are rebuilt on repository unload/load to avoid stale values.
- Graph edges are typed (e.g., Contains, DeclaresType/Member, Inherits, Implements, ImplementsMember, Calls, Changes, AuthoredBy, DependsOnPackage, ExternalReference, HasMetric, PropertyType, FieldType, ReturnType, ParameterType) to allow filtered traversal/degree. Edges are stored as DTOs (SourceId, TargetId, EdgeType, optional payload) for future serialization. Method/constructor metadata captures parameters (name, type DocId/SymbolKey, ordinal, optional) with edges to parameter types.

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
- Tree settings: annotation/filter settings and master toggle (`IdeTreeSettingsState`).
- Tabs: per-solution open files, active tab index, diff toggle state, commit metadata (`IdeTabsState`).
- Symbols: per-solution symbol popup state, active file, and symbol outline tree (`IdeSymbolsState`).
- Call stack: per-solution call stack popup state, call nodes, mermaid diagram, and selected node (`IdeCallStackState`).
- Decisions: per-solution pending/resolved lists, sorting, filtering, and dialog state (`IdeDecisionsState`).
- Layout: per-solution active main view and toolbox selection (`IdeLayoutState`).

Actions and effects:
- Load solutions, select solution, load tree root/children, open file tab, close tabs, toggle diff.
- Tree settings actions update annotation/filter state and reload tree nodes.
- Effects call API endpoints: `/api/repositories/loaded`, `/api/repositories/{id}/code/solutions/{solutionId}/tree` (with optional annotation/filter query params), `/api/repositories/{id}/code/solutions/{solutionId}/files`.
- Symbol popup effects call `/api/repositories/{id}/code/solutions/{solutionId}/files/symbols?path=...` and reload when the active tab changes while the popup is open.
- Call stack effects call `/api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack` and `/api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack/mermaid`.
- Decision effects call `/api/repositories/{id}/code/solutions/{solutionId}/decisions/pending`, `/resolved`, `/discover`, `/resolve`, `/notes`, `/activate`, and `/validate` to manage decision lifecycle and notes.

Components:
- `IdeSolutionSelector` uses solutions state and dispatches selection actions.
- `IdeTree` uses solutions + tree + tree settings state, renders annotation badges, and dispatches tree load and open-file actions.
- `IdeTabs` uses tabs state and dispatches close/toggle actions.
- `IdeSymbolPopup` renders the named type/member outline for the active file and dispatches symbol selection.
- `IdeCallStackPopup` renders the call stack tree and mermaid output and dispatches open-file actions for call sites.
- `IdeSymbolPopup` opens `IdeCallStackPopup` for the selected method when the user requests a call stack.
- `IdeMermaidViewer` renders Mermaid diagrams via the visualization host and JS interop.
- `IdeD3Viewer` is a minimal D3 viewer used to exercise the visualization host and palette pipeline.
- `DecisionToolboxContent` renders the Toolbox tabs for pending/resolved decisions and dispatches discovery actions.
- `PendingDecisionsPanel` and `ResolvedDecisionsPanel` render decision rows with sorting and filtering controls.
- `ResolveInterfaceDecisionControl` renders single-select candidate lists and dispatches resolve actions.
- `DecisionNotesDialog` edits notes for a decision and dispatches note updates.
- `IdeMenuBar` provides a single-select Toolbox menu that sets the active toolbox view (with a clear selection action).

### Git Domain

State:
- Commit metadata and diff lines stored on each open file tab (`IdeTabsState`).

Actions and effects:
- Toggle diff triggers `/api/repositories/{id}/git/files/{path}/last-change?includeDiff=true`.
- Initial tab open triggers `/api/repositories/{id}/git/files/{path}/last-change?includeDiff=false`.

Components:
- `IdeCommitCard` renders short SHA, date (UTC), author, author email, and diff toggle.
- `IdeTabs` renders annotated lines based on diff state.

### Decision Resolution (Interface Implementations)

Decision rules:
- When a service needs a concrete implementation for an interface method, it resolves candidates from solution compilations.
- If exactly one implementation exists, it is selected automatically.
- If multiple implementations exist but only one is outside test projects, that non-test implementation is selected.
- If multiple non-test implementations exist, a decision is required and must be provided by the user.

Decision persistence:
- Decisions are stored in the unified `Decisions` table (solution + repository scoped), with type-specific payload JSON.
- API endpoints: `/api/repositories/{id}/code/solutions/{solutionId}/decisions/pending`, `/resolved`, `/discover`, `/resolve`, `/notes`, `/activate`, and `/validate`.
- Decisions persist Roslyn documentation IDs for interface and implementation symbols when available, enabling stable symbol lookup across workspace reloads/compilations.
  Legacy `MethodImplementationDecisions` entries are read only for back-compat during the migration window.

Decision usage metadata:
- Services that use decisions must return decision metadata (id + type) in their responses; the field is present but null when no decision was applied.
- Current usage: `MethodCallStackService.BuildCallStackAsync` populates decision metadata on call stack nodes, returned via `MethodCallStackNodeDto.Decision` and `MethodCallStackNodeModel.Decision`.
- When a decision or candidate list is derived from documentation IDs, the response also carries the relevant documentation IDs alongside type names for follow-on lookups.

### Logging and Correlation

Client-side logging:
- Fluxor action dispatch is logged at Debug via `FluxorLoggingMiddleware`.
- Effects log Debug for start/end, Warning for recoverable failures, Error for terminal failures.

Correlation:
- All UI API calls include `X-Correlation-Id` (GUID) and log both the local id and response header value.
- API middleware echoes or creates `X-Correlation-Id` and decorates server logs with it.

## Visualization Rendering (Mermaid + D3)

SilkHat uses Mermaid and D3 for visualizations inside Blazor components (popups, tabs, and panels). Rendering runs via JS interop coordinated through a shared visualization host.

### Runtime rules

- Use a dedicated component per visualization (`IdeMermaidViewer`, `IdeD3Viewer`) backed by `VisualizationComponentBase`; never render directly in tab markup.
- Visualization components register a stable container id with `window.silkhatVisualHost` from `wwwroot/js/visualization-host.js`.
- Rendering must occur after the DOM element exists and has layout (`OnAfterRenderAsync`), and must be re-triggered when the host observes visibility or size changes (tabs/popup resize).
- JS interop must be idempotent: safe to register/unregister multiple times without leaking observers.
- If rendering fails or returns empty SVG, fall back to text output and log a warning.
- Mermaid/D3 scripts must load before the Blazor runtime initializes.
- Visualization renderers should accept theme palettes so diagrams match the current MudTheme.
- Theme palette data is produced by `VisualizationThemeService` as a `VisualizationThemePalette` DTO and passed to JS renderers (Mermaid and D3).

### Dependency rule

- The latest stable Mermaid and D3 builds must be installed and served locally from `wwwroot/js`.
- CDN usage is not permitted; keep versions pinned in the repo to avoid outages or version drift.

### Testing rules

- Add component-level tests that verify:
  - `silkhatVisualHost.register` is invoked when a viewer renders.
  - JS interop render is invoked when content is provided.
  - JS interop clear is invoked when content is empty.
  - A render request from the host triggers another render call.
  - Palette data is passed to the renderer.
- Add UI tests that ensure the viewer component is present in the popup/tab and that state changes trigger rendering.
- When adding a new visualization type, add a test that exercises a minimal dataset and verifies the renderer path is invoked.

## Documentation Maintenance

Whenever UI state, actions, effects, or components change, update the UI Architecture section above to reflect the current state structure, API endpoints, logging, and component usage.

## Test Stability Rule

Existing tests are treated as contracts for current behavior and should not be altered in what they assert or how they assert it. Updating mocks, wiring, or dependency setup is allowed when necessary to satisfy new implementation realities, but the intent and observable checks of existing tests must remain unchanged. New coverage should be added via new tests when behavior changes are introduced.
