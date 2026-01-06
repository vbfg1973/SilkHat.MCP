# M5f Solution-Scoped Code Endpoints

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this change, every code-analysis endpoint is scoped to a specific solution so users can target the correct solution when multiple solutions are loaded in the same repository group. The API and UI will accept a solutionId in routes, and solution-derived data such as code trees, projects, namespaces, named types, and symbol lookups will be served relative to that solution. Solution IDs are persisted alongside repository solution configs so the same identifier is used during navigation and loading.

## Progress

- [x] (2026-01-06 15:06Z) Inspected solution loading and identified single-solution assumptions in controllers, tests, and UI.
- [x] (2026-01-06 15:06Z) Chose solutionId derivation: use solution GUID from the .sln when present, otherwise use a deterministic GUID from the normalized relative path.
- [x] (2026-01-06 15:06Z) Extended workspace models to store per-solution indexes, tree entries, namespaces, named types, and compilations.
- [x] (2026-01-06 15:06Z) Updated API routes to include solutionId for code endpoints and added a code solutions listing endpoint.
- [x] (2026-01-06 15:06Z) Updated UI clients and IDE page to request solutions and pass solutionId for code requests.
- [x] (2026-01-06 15:06Z) Updated controller/service/UI tests to cover solution-scoped routes and solution listing.
- [x] (2026-01-06 19:10Z) Added persisted solutionId storage in repository solution configs and migrated the database schema.
- [x] (2026-01-06 19:10Z) Updated repository load workflows to use stored solutionIds and backfill missing IDs on load.
- [x] (2026-01-06 19:10Z) Updated repository discovery and UI configuration flows to carry solutionIds end-to-end.
- [x] (2026-01-06 22:09Z) Implemented hierarchical code tree queries with parentId filtering and registered FluentValidation for query validation.
- [x] (2026-01-06 22:09Z) Updated IDE to auto-select the first loaded repository/solution and render a hierarchical tree with lazy loading.
- [x] (2026-01-06 22:09Z) Added API/service/UI tests for tree hierarchy, query validation, and IDE tree behavior.
- [x] (2026-01-06 22:13Z) Sorted available solutions alphabetically before selecting the default solution in the IDE.
- [x] (2026-01-06 22:16Z) Aligned FluentValidation.AspNetCore package version with repository constraints (11.3.1).
- [x] (2026-01-06 22:17Z) Fixed IDE tree toggle to use typed mouse event callbacks for MudBlazor in bUnit.
- [x] (2026-01-06 22:28Z) Replaced the IDE tree list with MudTreeView and added lazy-loading callbacks that render names and icons correctly.
- [x] (2026-01-06 22:28Z) Added precomputed tree parent/child maps to solution workspaces for fast tree queries.
- [x] (2026-01-06 22:50Z) Fixed MudTreeView item click binding and tree parent path calculation build errors.
- [x] (2026-01-06 22:55Z) Ordered tree children with folders before files and added test coverage for the ordering.
- [x] (2026-01-06 22:59Z) Removed UI-side tree sorting so server ordering (folders-first) is preserved and fixed test helper path parsing.
- [ ] (2026-01-06 22:14Z) Run `dotnet test` and confirm all non-infrastructure tests pass (attempted; MSBuild failed to create named pipe due to permission denied).
- [ ] (2026-01-06 22:14Z) Run `docker compose up --build` and confirm API + UI run in Docker (attempted; blocked by Docker daemon socket permissions).
- [x] (2026-01-06 15:08Z) Wrote Milestone Commit Notes for M5f and updated milestone state files.

## Surprises & Discoveries

- Observation: `dotnet test` failed because MSBuild could not create its named pipe in this environment.
  Evidence: `System.Net.Sockets.SocketException (13): Permission denied` while binding the MSBuild node pipe.
- Observation: `docker compose up --build` fails unless `REPO_ROOT` is set.
  Evidence: `required variable REPO_ROOT is missing a value`.
- Observation: Docker daemon access is blocked in this environment.
  Evidence: `permission denied while trying to connect to the Docker daemon socket`.

## Decision Log

- Decision: Use the .sln SolutionGuid as the solutionId when present, otherwise use a deterministic GUID derived from the normalized relative solution path.
  Rationale: The SolutionGuid provides a stable identifier when available, while the deterministic fallback ensures consistency across reloads without external tooling.
  Date/Author: 2026-01-06 15:06Z / Codex
- Decision: Persist solutionId in `RepositorySolutionConfig` and use it as the source of truth during repository loads.
  Rationale: Storing the identifier guarantees consistent navigation IDs across API, UI, and workspace reloads while still allowing fallback computation for missing values.
  Date/Author: 2026-01-06 19:10Z / Codex
- Decision: Serve hierarchical tree nodes by filtering stored tree entries by `DisplayPath` and `parentId`.
  Rationale: Existing tree entries already encode fully qualified display paths, enabling direct-child filtering without recomputing tree structure.
  Date/Author: 2026-01-06 22:09Z / Codex
- Decision: Default the IDE solution selection to the alphabetically first solution name (tie-breaking by relative path).
  Rationale: The UI should consistently choose the first solution when multiple solutions are available, matching the alphabetical selection rule.
  Date/Author: 2026-01-06 22:13Z / Codex
- Decision: Precompute a display-path keyed parent/child map for code tree entries during solution load.
  Rationale: The tree map supports fast, lazy tree lookups without repeated path scans, while keeping the API contract unchanged.
  Date/Author: 2026-01-06 22:28Z / Codex

## Outcomes & Retrospective

Implemented solution-scoped workspaces and routes plus UI selection for solution-specific code data, persisted solutionId values in repository solution configs, and added hierarchical tree queries with lazy-loading in the IDE. Test and Docker validation remain blocked in this environment due to MSBuild named pipe permissions and Docker daemon access restrictions (also requires `REPO_ROOT`).

## Context and Orientation

Code analysis is loaded from solution files and stored in `CodeRepositoryWorkspace` within `src/SilkHat.Code.Analysis`. Current API endpoints in `src/SilkHat.Api/Controllers` expose code projects, namespaces, named types, symbol lookup, and code tree data without a solution identifier. The UI in `src/SilkHat.Ui` queries these endpoints and assumes a single solution. This milestone introduces solution identifiers and updates all solution-derived endpoints to include a solutionId in the route so that multiple solutions can be addressed explicitly.

## Plan of Work

First, inspect how solution paths are resolved and stored in the code workspace loader to identify where to attach solution identity. Next, define a solutionId scheme that is stable: if a stable ID can be read from the solution file, use it; otherwise, derive a deterministic ID from the solution path. Then extend the workspace model to store solution metadata and ensure code indexes can be filtered by solution. Update persistence so repository solution configs store the chosen solutionId, and update repository discovery to surface IDs when listing solutions. Update all API routes that return solution-derived data to include the solutionId and adjust controllers to retrieve the correct solution-scoped data. Update UI clients and pages to include solutionId in requests, and adjust tests to cover the new routing and filtering behavior. Finally, run tests and Docker to validate end-to-end behavior.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build -d

## Validation and Acceptance

Run `dotnet test` and expect all tests to pass. Start Docker and verify:

- Code endpoints require a solutionId and return solution-specific data.
- UI can select and query a specific solution and renders the correct tree and symbols.
- Multiple solutions in a repository group are distinguishable by solutionId.

## Idempotence and Recovery

Route changes are safe to reapply. If solutionId derivation changes, update tests and regenerate any affected data. Ensure migration scripts are not required for this change.

## Artifacts and Notes

Record the final solutionId derivation rules and any edge cases encountered in solution parsing.

## Interfaces and Dependencies

Define solution identifiers in `src/SilkHat.Code.Analysis` (likely in the solution parser or workspace loader). Update `RepositorySolutionConfig` to persist `SolutionId` and include it in API DTOs. Update `CodeRepositoryWorkspace` to carry solution metadata and solution-scoped indexes. Update API controllers for code endpoints to accept a `solutionId` route parameter and to load code workspaces using stored solutionIds. Update UI client methods and IDE views to pass solutionId for all solution-derived calls.

## Test Plan

Add or update tests in `tests/SilkHat.Api.Tests`, `tests/SilkHat.Tests`, and `tests/SilkHat.Ui.Tests` to validate solution-scoped routes. Ensure controller tests verify correct filtering by solutionId, service tests verify solution mapping logic, and UI tests confirm that the solution selection drives the correct API calls.

Plan update (2026-01-06 15:06Z): Updated progress and decision log after implementing solution-scoped workspace and endpoint changes.
Plan update (2026-01-06 15:07Z): Recorded test and docker validation attempts with environment failures.
Plan update (2026-01-06 15:08Z): Updated milestone commit notes and progress state.
Plan update (2026-01-06 15:09Z): Added Docker socket permission failure to validation notes.
Plan update (2026-01-06 19:10Z): Extended scope to persist solutionId in repository configs and updated progress/decisions.
Plan update (2026-01-06 19:15Z): Recorded reattempted test and Docker runs with the same environment failures.
Plan update (2026-01-06 22:09Z): Extended milestone scope to cover hierarchical tree queries and IDE auto-load behavior.
Plan update (2026-01-06 22:13Z): Recorded alphabetical solution selection change for IDE defaults.
Plan update (2026-01-06 22:14Z): Re-ran tests and Docker compose; failures unchanged due to MSBuild pipe and Docker socket permissions.
