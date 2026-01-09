# M15: Decision system + toolbox UI for pending and made decisions

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root and `.agents/PLANS.md`. It must remain fully self-contained.

## Purpose / Big Picture

After this change, the system will provide a unified decision service that can persist, query, and validate decisions across multiple decision types. The IDE will expose a Toolbox panel with tabs that list pending decisions and made decisions, enabling users to select interface implementations, toggle decisions active/inactive, and attach notes. Users will be able to discover decision requirements, manage them via the API, and see the decision UI update accordingly. The behavior is observable by running the API endpoints and by using the Toolbox UI to resolve interface decisions in the sample repository.

## Progress

- [x] (2026-01-09 17:38Z) Create ExecPlan for M15 with decision storage, discovery, and toolbox UI workflow.
- [x] (2026-01-09 20:06Z) Added unified decision DTOs, entity, configuration, and EF migration for decision persistence.
- [x] (2026-01-09 20:06Z) Implemented decision service and API controller for discovery, listing, resolve, notes, activation, and validation.
- [x] (2026-01-09 20:06Z) Implemented Toolbox UI components and Fluxor state/actions/effects for decision management.
- [x] (2026-01-09 20:06Z) Added unit tests for decision services, controller endpoints, and decision toolbox components.
- [x] (2026-01-09 20:06Z) Validation: `dotnet test` passes after restricting decision discovery to source-defined interfaces.
- [x] (2026-01-09 23:39Z) Adjusted toolbox selection to a single-select menu item and tuned repository tree font sizing.
- [ ] (2026-01-09 20:06Z) Docker validation pending: `docker-compose up --build` failed during UI image export with a missing snapshot error.

## Surprises & Discoveries

- `dotnet test tests/SilkHat.Api.Tests/SilkHat.Api.Tests.csproj` previously hung because decision discovery enumerated interface symbols from referenced assemblies; limiting discovery to source-defined interfaces resolved the hang.
  Evidence: After filtering on `type.Locations.Any(IsInSource)`, `dotnet test` completes and `SilkHat.Api.Tests` reports 86 passed.
- `docker-compose up --build` failed during UI image export with “failed to prepare extraction snapshot ... parent snapshot ... does not exist”.
  Evidence: `target ui: failed to solve: failed to prepare extraction snapshot ... parent snapshot ... does not exist`.
- `docker-compose up --build` continued to fail after a build cache prune, now during API image export with the same snapshot error.
  Evidence: `target api: failed to solve: failed to prepare extraction snapshot ... parent snapshot ... does not exist`.

## Decision Log

- Decision: Decision type-specific data is stored as JSON with a `DecisionType` discriminator and hydrated into concrete decision data models via a registry.
  Rationale: The decision types are extensible and schema is not known in advance. JSON payloads allow forward-compatible storage while enabling typed models in code.
  Date/Author: 2026-01-09 / Codex
- Decision: Use a unified decision service with generic methods and type-specific resolvers registered in DI.
  Rationale: A single API surface simplifies management and discovery across all decision types while still allowing typed handling.
  Date/Author: 2026-01-09 / Codex
- Decision: Provide both pending and made decisions via separate API endpoints and UI tabs; decisions can be toggled active/inactive without deletion.
  Rationale: This matches the workflow requirements and keeps the decision history visible.
  Date/Author: 2026-01-09 / Codex
- Decision: Restrict decision discovery to interface types defined in source (`IsInSource`) to avoid scanning referenced assemblies.
  Rationale: Enumerating metadata interface types from referenced assemblies caused test hangs and is not relevant to solution decisions.
  Date/Author: 2026-01-09 / Codex

## Outcomes & Retrospective

- Completed unified decisions storage/service/UI plus toolbox menu selection, with documentation updates and passing tests. Docker compose validation remains blocked by a snapshot error during image export.

## Context and Orientation

Decisions currently exist only for interface implementation resolution via `MethodImplementationDecisions` and `MethodImplementationDecisionService`. Decisions are returned on call stack nodes with metadata. Decision rules and persistence are documented in `docs/architecture.md` under “Decision Resolution (Interface Implementations)”. There is no UI for managing decisions outside in-context call stack resolution. The UI now includes a Toolbox area, but it does not host a decisions view. The API has `MethodImplementationDecisionsController` for interface decisions, but there is no unified decision management or discovery of pending decisions.

A “decision” is a persisted record that captures a user choice required by analysis logic. A “pending decision” is a record that has been discovered as needed but is not yet resolved. “Made decisions” are resolved records, which may be active or inactive. “Decision type-specific data” refers to data fields unique to a decision type, such as interface method targets and candidate implementations.

## Plan of Work

First, expand the persistence model to support multiple decision types and type-specific data. Introduce a new table `Decisions` (or repurpose existing `MethodImplementationDecisions` into a general `Decisions` table) that stores common metadata and a JSON payload for type-specific data. The common fields include:

- `Id` (GUID)
- `DecisionType` (enum, starting with `ResolveInterface`)
- `RepositoryConfigId`
- `SolutionId`
- `Status` (Pending, Resolved)
- `IsActive` (bool for resolved decisions)
- `CreatedUtc` and `UpdatedUtc` (BaseEntity)
- `DiscoveredUtc` (when the need was detected)
- `ResolvedUtc` (nullable)
- `Notes` (nullable text)
- `PayloadJson` (type-specific data)

Create a `DecisionPayload` base model in code, plus concrete payload models for `ResolveInterface`. The payload for `ResolveInterface` must contain the interface type DocID, interface method DocID, candidate implementation type DocIDs and method DocIDs, and the selected implementation DocID (nullable). Use the existing doc ID storage logic already introduced in M10–M14.

Use EF Core value conversion to map the payload JSON to a `DecisionPayload` type. Implement a `DecisionPayloadRegistry` that maps `DecisionType` to a concrete payload .NET type and JSON serializer options. The registry is used by a common `DecisionSerializer` helper to deserialize/serialize the payload. This avoids hardcoding decision types across the service. Use `System.Text.Json` with explicit type handling; do not use runtime polymorphic JSON features that require metadata in the JSON itself.

Second, implement a common decision service. Create an interface `IDecisionService` in `src/SilkHat.Code.Analysis/Abstractions` and a concrete `DecisionService` in `src/SilkHat.Code.Analysis/Services`. The service should expose:

- `DiscoverPendingAsync` for a decision type and/or for all types; this scans the solution and stores new pending decisions.
- `GetPendingAsync` and `GetResolvedAsync` with filtering and sorting (by age, type, and status).
- `SetDecisionAsync` to resolve a pending decision or update an existing resolved decision.
- `SetActiveAsync` to toggle active/inactive.
- `ValidateAsync` to check DocIDs exist for all types referenced by the payload; set `IsValid` flag on the decision.
- `UpdateNotesAsync` for notes dialog.

“Discover pending” for `ResolveInterface` should traverse the solution for interface method calls that require a decision. The logic should reuse or call into the existing interface resolution pipeline in `MethodImplementationDecisionService`, but it must not auto-select; it should record candidates and create pending decisions. Store the pending decisions with `DecisionType=ResolveInterface` and `Status=Pending` and leave `SelectedImplementation` unset.

Third, expose API endpoints for decision management. Add a `DecisionsController` with routes under `/api/repositories/{id}/code/solutions/{solutionId}/decisions`. Provide endpoints:

- `GET /pending` with optional query params `type`, `sort=age|type`, `direction=asc|desc`.
- `GET /resolved` with same filters and an `active` filter.
- `POST /discover` to run discovery for all types or a single type.
- `POST /resolve` to set/replace a decision (payload includes decision id and type-specific selection).
- `POST /notes` to set notes for a decision.
- `POST /activate` to toggle active/inactive.
- `POST /validate` to revalidate a decision.

Use DTOs that separate metadata from payload. The payload should be serialized by decision type and returned as typed DTOs (e.g., `ResolveInterfaceDecisionPayloadDto`). For unknown decision types, return the raw payload JSON and the type enum so the UI can still display it generically.

Fourth, implement the Toolbox UI with tabs. The Toolbox component will be the first toolbox content and must be composed of subcomponents:

- `DecisionToolbox` (container with tabs)
- `PendingDecisionsPanel`
- `ResolvedDecisionsPanel`
- `DecisionNotesDialog`
- `ResolveInterfaceDecisionControl` (type-specific control)

The Toolbox should appear in the IDE Toolbox area with a short tab label (e.g., “Decisions”). Inside, the tabbed UI should show “Pending” and “Resolved” tabs (short labels, tooltips for full descriptions). Each panel lists decisions with collapsible rows. Default state is collapsed. Expand in place (preferred), with an option to open a dialog for a full editor view. The rows must show decision type, target name (interface type name for ResolveInterface), and discovery/resolve timestamps. Provide sorting and filtering by type; keep controls compact with icons and tooltips.

For `ResolveInterface`, the expanded control lists candidate implementations with a single-selection radio option. Default selection is none. A “Save” button resolves the decision (calling the API). Provide a “Clear” action to unselect. The resolved list shows the selected implementation and allows toggling active/inactive, plus opening the notes dialog.

Fifth, update UI architecture documentation. The `docs/architecture.md` section “Decision Resolution” must be updated to describe the unified decision service, payload storage pattern, and the Toolbox decisions UI, including endpoints used. Add the decision discovery/validation rule and mention that decisions now have a status (pending/resolved) and active flag.

## Concrete Steps

1) Add core models and persistence
   - Create `DecisionType` enum (starting with `ResolveInterface`), `DecisionStatus` enum, and a `Decision` entity derived from `BaseEntity` with the fields described above.
   - Create payload models and DTOs for `ResolveInterface` and a base payload interface.
   - Add EF configuration for `Decision` and the JSON payload converter. Create migration.

2) Implement the decision service
   - Add `IDecisionService` in `src/SilkHat.Code.Analysis/Abstractions` and `DecisionService` in `src/SilkHat.Code.Analysis/Services`.
   - Add `DecisionPayloadRegistry` and JSON serialize/deserialize utilities.
   - Add a discovery method for `ResolveInterface` that builds pending decisions from candidate interface method implementations.

3) Add API endpoints
   - Create `DecisionsController` with endpoints listed above.
   - Add DTOs for list responses and type-specific payloads in `src/SilkHat.Code.Core/Dtos`.
   - Wire the new service in `src/SilkHat.Api/Program.cs`.

4) Implement UI components
   - Add Fluxor state slice for decisions (pending/resolved lists, filters, sorting, active toggles).
   - Add actions/effects to call decision endpoints.
   - Build Toolbox UI components and integrate into IDE Toolbox area.
   - Add notes dialog component.

5) Update documentation and tests
   - Update `docs/architecture.md` with decision service + UI.
   - Add unit tests for decision payload serialization and discovery logic.
   - Add controller tests for each decision endpoint.
   - Add bUnit tests for toolbox panels to verify list rendering, filtering, and selection behavior.

6) Run validation
   - `dotnet test`
   - `REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose up --build -d`
   - `REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose down`

## Validation and Acceptance

- `dotnet test` passes.
- Decision discovery endpoint creates pending decisions for interface methods with multiple non-test implementations in the sample repository (or test fixture repository).
- Pending decisions appear in the Toolbox, are collapsible, and can be filtered by type.
- Selecting a candidate and saving creates/resolves a decision; it appears in the “Resolved” tab with timestamps and active toggle.
- Notes dialog updates notes in the API and is reflected in the UI.
- Validation marks decisions valid/invalid based on DocID existence.

## Idempotence and Recovery

Discovery is idempotent. If a pending decision already exists for a given interface method docId, do not create a duplicate; update its candidate list if necessary. Resolved decisions can be toggled active/inactive without deletion. JSON payload versioning should be tolerant of unknown fields to support future decision types.

## Artifacts and Notes

Example DocIDs for ResolveInterface payloads will use the existing patterns from call stack nodes, such as:

    Interface type DocID: T:Namespace.IInterface
    Interface method DocID: M:Namespace.IInterface.Method(System.String)
    Implementation type DocID: T:Namespace.ConcreteType
    Implementation method DocID: M:Namespace.ConcreteType.Method(System.String)

## Interfaces and Dependencies

In `src/SilkHat.Code.Core/Dtos`, define:

    public enum DecisionType { ResolveInterface = 1 }
    public enum DecisionStatus { Pending = 1, Resolved = 2 }

    public sealed record DecisionSummaryDto(
        Guid Id,
        DecisionType Type,
        DecisionStatus Status,
        bool IsActive,
        string Name,
        DateTimeOffset DiscoveredUtc,
        DateTimeOffset? ResolvedUtc,
        bool IsValid,
        string? Notes,
        object? Payload);

    public sealed record ResolveInterfaceDecisionPayloadDto(
        string InterfaceTypeDocId,
        string InterfaceMethodDocId,
        IReadOnlyList<string> CandidateTypeDocIds,
        IReadOnlyList<string> CandidateMethodDocIds,
        string? SelectedTypeDocId,
        string? SelectedMethodDocId);

In `src/SilkHat.Code.Analysis/Abstractions`, define:

    public interface IDecisionService
    {
        Task<IReadOnlyList<DecisionSummaryDto>> DiscoverPendingAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            DecisionType? type,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<DecisionSummaryDto>> GetPendingAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            DecisionType? type,
            string? sort,
            bool descending,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<DecisionSummaryDto>> GetResolvedAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            DecisionType? type,
            bool? active,
            string? sort,
            bool descending,
            CancellationToken cancellationToken);

        Task<DecisionSummaryDto?> ResolveAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid decisionId,
            object payload,
            CancellationToken cancellationToken);

        Task<DecisionSummaryDto?> SetActiveAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid decisionId,
            bool isActive,
            CancellationToken cancellationToken);

        Task<DecisionSummaryDto?> SetNotesAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid decisionId,
            string? notes,
            CancellationToken cancellationToken);

        Task<DecisionSummaryDto?> ValidateAsync(
            CodeRepositoryWorkspace workspace,
            CodeSolutionWorkspace solution,
            Guid decisionId,
            CancellationToken cancellationToken);
    }

## Test Plan

- Add unit tests for decision payload serialization/deserialization for `ResolveInterface` and for unknown decision types.
- Add unit tests for discovery logic to ensure pending decisions are created only when multiple non-test implementations exist.
- Add controller tests for each endpoint to verify dependency calls and responses.
- Add bUnit tests for pending/resolved panels: list rendering, type filter, sorting, expand/collapse, selection, save, active toggle, and notes dialog.
- Run `dotnet test`, then docker compose up/down with `REPO_ROOT=/home/vbfg/repos/c/dev/repos/`.

Plan Update Notes: 2026-01-09 — Initial M15 ExecPlan drafted for unified decisions and Toolbox UI.
Plan Update Notes: 2026-01-09 — Progress updated with implemented decision storage/service/UI/tests and noted the `SilkHat.Api.Tests` hang during validation.
Plan Update Notes: 2026-01-09 — Logged docker-compose UI image export failure during validation.
Plan Update Notes: 2026-01-09 — Marked test validation as passing after limiting decision discovery to source interfaces and recorded the decision.
Plan Update Notes: 2026-01-09 — Documented toolbox single-select menu update and repository tree font tuning.
