# M19: Annotate and filter repository tree (server-backed, lazy-friendly)

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds. Maintain this plan in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this change a user can open the repository tree and: (1) see badges on projects, folders, and files that show complexity or git-based metrics; (2) filter the tree so only nodes matching chosen thresholds remain visible; (3) toggle annotations/filters on or off; and (4) configure these via small hover-revealed icons. Filtering and annotation data is computed server-side so large trees remain responsive, and filters prune nodes the server does not return.

## Progress

- [x] Draft and align API, UI, and data model changes for annotated/filtered trees.
- [x] Implement server-side metrics aggregation and filtered tree queries.
- [x] Implement Fluxor state/actions/effects for annotations and filters; add UI icons and dialogs.
- [x] Render annotations with badges and honor filters in the tree component.
- [x] Add tests (unit, API, UI) and documentation updates.
- [x] Extend plan/implementation for precomputed, near-instant annotations/filters.
- [x] Enforce repository tree scrollability (vertical + horizontal) under expansion.
- [x] Adjust tree typography/badge layout (9pt nodes, 8pt badges, no clipping via z-index/padding).
- [x] Enable file-node expansion into types and members with complexity annotations (git remains file-level).
- [x] Ensure file author counts roll up as distinct authors across folders/projects.

## Surprises & Discoveries

- Annotation/filter toggles can leave MudTreeView expansion state stale; forcing a component re-key on settings changes avoids non-expandable trees.
- File author counts need distinct-author rollups (union sets), not sum-of-counts, for folder/project aggregates.

## Decision Log

- Metrics are computed via `CodeTreeMetricsService` and cached in `CodeTreeMetricsCacheStore`. Precompute runs per repository load using git/complexity aggregators for near-instant tree annotations/filters.
- Need near-instant annotations/filters at interaction time: plan to precompute all metrics once per repo load (see below) with single-pass git ingestion and parallel complexity aggregation; serve trees from cached maps only; invalidate on reload/unload.

## Outcomes & Retrospective

- Repository tree supports server-side annotations/filters with cached metrics and precompute on load.
- Tree nodes now expand for files → types → members, with method/constructor highlighting on file open.
- Tree scrollability, spacing, and badge layout are stable under expansion.
- Git author metrics correctly dedupe at folder/project levels via distinct-author sets.

## Context and Orientation

The repository tree is served by `src/SilkHat.Api/Controllers/Code/CodeTreeController.cs`, which calls `ICodeTreeService` to return `CodeTreeEntryDto` results; queries are currently unfiltered except for parent/paging. The UI tree is `src/SilkHat.Ui/Components/Ide/IdeTree.razor`, driven by Fluxor state in `src/SilkHat.Ui/State/Ide/IdeTreeState.cs` with actions/effects in `IdeTreeActions/Effects/Reducers`.

Metrics needed:
- Complexity (cognitive, cyclomatic, indentation): currently computed per type/method; file-level sum must be derived from types in a file; folder/project need recursive sums.
- Git: file change count (already available via Git API) and file author count (not yet exposed).

Annotations are to be displayed as `MudBadge` on tree nodes (projects, folders, files). Filtering should hide nodes that do not meet criteria, with filtering performed server-side so lazy loading remains efficient. UI needs small hover-only icons inside the tree control to open dialogs for Annotation and Filter, plus a master toggle to enable/disable both.

## Plan of Work

Describe the combined backend and UI approach:

1) Extend server-side tree support
- Add annotation metadata to `CodeTreeEntryDto` (e.g., optional `AnnotationValue` and `AnnotationKind`) and carry it through to UI models.
- Add a `CodeTreeFilterQuery` to the API that includes: selected metric (enum: Cognitive, Cyclomatic, Indentation, FileAuthorCount, FileChangeCount), comparison operator (e.g., <= / >), and threshold. Include a flag to disable filters. Continue to accept parent/paging.
- Add optional annotation kind selection in the query so the server computes and returns the chosen metric for each node.
- Update `ICodeTreeService` and implementation to:
  - Compute per-file metrics on demand (complexity sums, git file change count, git author count) and cache them per solution/workspace.
  - Aggregate metrics recursively for folders and projects.
  - Apply filters server-side when a filter is present, pruning non-matching nodes; ensure child loading honors the same filter.
- Update `CodeTreeController` to accept the new query and return annotated/filtered results; ensure validation errors return ProblemDetails.

2) Add git author count support
- Add a Git API to retrieve distinct author count for a file on the current branch (extend `IGitCli` and controller) so tree aggregation can use it during precompute.

3) UI state and effects
- Add new Fluxor state slice (or extend `IdeTreeState`) to store:
  - Selected annotation kind
  - Filter definition (metric, operator, threshold, enabled flag)
  - Master toggle for annotations/filters
- Add actions/effects to update state from dialogs and to pass filter/annotation choices into tree API requests.
- Ensure tree requests include the filter/annotation query params; when filters are disabled, request the full tree without filter.

4) UI components and dialogs
- In `IdeTree`, add a tiny hover-only control bar (inside the component) with three icons: Annotation dialog, Filter dialog, Enable/Disable toggles.
- Build dialogs for Annotation (single select metric) and Filter (metric, operator, threshold, enable).
- Render annotations using `MudBadge` on tree nodes when annotation is enabled; hide badges when disabled.
- Filtering: when enabled, only nodes returned from the server (already filtered) are shown; ensure lazy loading uses the same filter state.

5) Lazy-loading and data considerations
- Ensure child fetches always include the current filter/annotation query to avoid mixing filtered and unfiltered nodes.
- Keep paging support; if filtering returns many matches, the server can still page results.

6) Documentation
- Update `docs/architecture.md` UI section to describe the new tree state (annotation/filter), the new API query, and the hover icon behavior.
- Note caching/precompute of metrics on solution load.
- Note cached, on-demand metrics and server-side filter behavior.
- Add notes on nested type/member nodes, scroll behavior expectations, and font/badge sizing.

## Extension: Near-instant annotations/filters via precompute

Goal: tree requests are O(lookup) only. Precompute metrics once per repo load, then serve annotations/filters from cache. Invalidate on unload/reload.

Planned changes:

1) Metric cache store
- Add `ICodeTreeMetricsCacheStore` to hold per `(configId, solutionId)` bundles:
  - For each metric kind: `FileMetrics` (path->int) and `NodeMetrics` (path->int aggregated to folders/projects/files)
  - Readiness flags per metric.
- Clear entries on repo unload/load; also call `IApiCache.InvalidateAll()` to drop other dependent caches.

2) Git metrics: single-pass ingestion
- Implement `IGitMetricsAggregator` that runs one `git log` pass per repo to produce:
  - Change counts per file (from `--numstat`)
  - Distinct author counts per file (collect author per path)
- Normalize to repo-relative paths (`./...`), store in dictionaries.

3) Complexity metrics: parallel aggregation
- Implement `IComplexityMetricsAggregator`:
  - Parallel syntax-based walkers over solution documents (avoid per-file semantic model where possible).
  - Compute cognitive/cyclomatic/indentation per file.
  - Normalize to repo-relative paths.

4) Aggregation to folders/projects
- After per-file metrics exist, build `NodeMetrics` by propagating file values up parent paths (projects/folders/files) for each metric kind.

5) Orchestration
- After repository load completes, trigger background aggregation per solution:
  - Run git aggregator once per repo (shared across solutions).
  - Run complexity aggregator per solution (using its workspace).
  - Aggregate to folders/projects; store results + readiness flags in cache.
- Optionally mark metrics “pending” until ready; filters disabled until metric ready.

6) Serving requests
- `CodeTreeQueryService` reads from cache only:
  - If requested metric not ready: return unannotated tree or a controlled Problem/empty based on policy.
  - Filtering uses cached file metrics; annotations use cached node metrics.
- Controllers remain thin and async.

7) Validation/tests
- Add unit tests for aggregators (folder/project sums).
- Test query service with cached metrics (filter + annotation paths) and “metric not ready” behavior.
- Test git aggregator parsing with sample log.
- Integration: cache invalidation on load/unload.

8) Docs
- Update architecture to describe precompute, single-pass git ingestion, parallel complexity aggregation, cache store, and readiness policy.
- Note cached, on-demand metrics and server-side filter behavior.

## Concrete Steps

Working directory for commands: `/home/vbfg/RiderProjects/SilkHat.MCP`

1. Add DTO/query changes:
   - Update `src/SilkHat.Api/Models/CodeTreeQuery.cs` (or add a new query model) with annotation/filter fields.
   - Update `src/SilkHat.Code.Core/Dtos/CodeTreeEntryDto.cs` (and UI models) to carry optional annotation value/kind.

2. Backend aggregation and filtering:
   - Update `ICodeTreeService` and `CodeTreeService` to precompute metrics at solution load and aggregate per folder/project.
   - Add server-side filtering logic that prunes nodes before returning.
   - Ensure the controller passes query data to the service and returns badges data.

3. Git author count:
   - Extend `IGitCli` and add an API endpoint to get distinct author count for a file; wire into precompute.

4. UI state and effects:
   - Add Fluxor actions/reducers/effects to store annotation kind, filter, enable flags.
   - Update `IdeTreeEffects` to include the query params on API calls.

5. UI components:
   - Add hover-only icon bar in `IdeTree.razor`.
   - Add Annotation and Filter dialogs; add master toggle.
   - Render `MudBadge` on tree nodes when enabled.

6. Tests:
   - Unit tests for aggregation and filtering in `CodeTreeService`.
   - API controller tests for annotation/filter query behavior.
   - Git author count tests (unit/API).
   - UI/Fluxor tests: state reducers/effects, and component render showing badges/filtered nodes and icon hover behavior (as feasible in bUnit).

7. Validation:
   - Run `dotnet test`.
   - Manual check: run API + UI (docker compose), enable annotation/filter in the tree, confirm badges render and filtered nodes disappear, and toggles work.

## Validation and Acceptance

Acceptance criteria:
- A user can open the Annotation dialog, choose a metric, and see badges on projects/folders/files showing the aggregated value.
- A user can enable a filter (metric, operator, threshold); the tree shows only matching nodes (server-side filtering) and lazy-loaded children respect the filter.
- The master toggle hides annotations and disables filters together; restoring it re-applies current selections.
- Hovering over the tree reveals the small icon bar; icons are no larger than tree icons.
- Tests pass via `dotnet test`; manual UI check shows badges and filtering behavior in the running app.

## Idempotence and Recovery

All changes are additive and guarded by feature state; rerunning tests and reloads are safe. If filtering leads to an empty tree, disabling the filter restores the original tree. Precompute runs on solution load; if it fails, log and fall back to unannotated/unfiltered data.

## Artifacts and Notes

Keep query examples for future reference:

  - GET `/api/repositories/{id}/code/solutions/{solutionId}/tree?parentId=...&annotationKind=CognitiveComplexity&filterMetric=CognitiveComplexity&filterOp=lte&filterThreshold=50`

Plan Update Notes: 2026-01-10 — Initial draft for M19 (tree annotations and filtering).
Plan Update Notes: 2026-01-10 — Added scroll/typography fixes and nested type/member expansion with complexity annotations.
Plan Update Notes: 2026-01-10 14:59Z — Completed tree expansion/spacing fixes and distinct-author rollups for file author count.
