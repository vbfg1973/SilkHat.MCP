# M4a Testing Standardization and Controller Organization

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone establishes a repeatable testing standard for controllers and services while keeping the codebase maintainable as the API grows. After completion, every controller endpoint will have tests that verify dependency interactions and responses, services will have unit and integration coverage with edge cases, and the code analysis endpoints will be organized into focused controllers to support future expansion.

## Progress

- [x] (2026-01-05 13:59Z) Split code analysis endpoints into focused controllers while preserving routes and behaviors.
- [x] (2026-01-05 14:50Z) Defined controller testing patterns for dependency calls and response assertions by introducing mockable store interfaces and test helpers.
- [x] (2026-01-05 14:50Z) Added controller tests for all existing endpoints following the new standard.
- [x] (2026-01-05 14:50Z) Defined service unit and integration testing patterns (including edge cases).
- [x] (2026-01-05 14:50Z) Added service unit tests and integration tests to meet the standard.
- [x] (2026-01-05 14:50Z) Ran `dotnet test` and verified all non-infrastructure tests pass.
- [x] (2026-01-05 14:50Z) Ran `docker compose up --build` and confirmed API + UI run in Docker.
- [ ] Write Milestone Commit Notes for M4a and update milestone state files.

## Surprises & Discoveries

- Observation: `dotnet test` failed with MSBuild named pipe permissions until the MSBuild server was disabled.
  Evidence: `MSBUILD : error MSB1025 ... SocketException (13): Permission denied` on default `dotnet test`.
- Observation: UI nginx returned a redirect loop until an explicit `root /usr/share/nginx/html;` was added.
  Evidence: `rewrite or internal redirection cycle while internally redirecting to "/index.html"` in nginx logs.
- Observation: API container crashed on startup due to incompatible OpenApi assemblies.
  Evidence: `TypeLoadException ... Could not load type 'Microsoft.OpenApi.Any.IOpenApiAny'` on container startup.
- Observation: SymbolKey reflection was unavailable in test runs and required a fallback to keep code analysis functional.
  Evidence: `InvalidOperationException: SymbolKey support is not available` before adding a fallback in `SymbolKeyUtility`.

## Decision Log

- Decision: Split code analysis endpoints into focused controllers per domain (projects, namespaces, named types, symbols).
  Rationale: Each domain is expected to expand, and separating controllers keeps routing, filters, and tests focused.
  Date/Author: 2026-01-05 / Codex
- Decision: Drop the direct `Microsoft.AspNetCore.OpenApi` package reference to avoid OpenApi assembly conflicts with Swashbuckle.
  Rationale: The API container failed at runtime due to missing OpenApi types, and Swashbuckle already brings the needed OpenApi dependency.
  Date/Author: 2026-01-05 / Codex
- Decision: Introduce store interfaces so controller tests can verify dependency calls.
  Rationale: Controllers depended on concrete stores, which made call verification impossible without interfaces.
  Date/Author: 2026-01-05 / Codex
- Decision: Use a SymbolKey fallback when reflection is unavailable.
  Rationale: Keeps code analysis functioning in environments where SymbolKey extensions are missing.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M4a established controller and service testing standards, added unit/integration coverage across services, and ensured Docker + test validation succeeded. The next milestone (M4b) will focus on UI hosting fixes and any remaining UI issues.

## Context and Orientation

API controllers live in `src/SilkHat.Api/Controllers`. Code analysis endpoints currently share a single controller and are backed by `CodeWorkspaceStore` from `src/SilkHat.Code.Analysis`. Tests live under `tests/SilkHat.Api.Tests` and `tests/SilkHat.Tests`. This milestone adds a testing standard that validates dependency interactions and responses for controllers, plus unit and integration tests for services, and re-organizes the code analysis endpoints into focused controllers while preserving the existing route structure.

## Plan of Work

First, split the existing code analysis controller into multiple controller classes by endpoint domain: projects, namespaces, named types, and symbol lookup. Keep route prefixes aligned with the existing `/api/repositories/{id}/code/...` structure, and keep error handling identical. Next, define a testing approach: controller tests should assert both response payloads and that dependencies are called, while service unit tests should mock dependencies to assert calls and edge cases. Integration tests should exercise real service behavior without external infrastructure. Add tests for all existing controllers and services to match the standard. Finally, run `dotnet test` and `docker compose up --build`, and capture results in milestone state files and commit notes.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` and expect all tests that do not require external infrastructure to pass. Run `docker compose up --build` and confirm:
1) API responds to `/api/health`.
2) UI loads and can call the API health endpoint.
3) Code analysis endpoints are available at the same routes as before, now grouped by controller.

Each controller endpoint should have tests that verify dependency calls and response behavior. Each service should have unit tests for dependency interactions and edge cases, plus integration tests covering real behavior.

## Idempotence and Recovery

Controller re-organization and tests are additive and can be re-run safely. If Docker builds fail, rerun after clearing any previous containers with `docker compose down` to reset state.

## Artifacts and Notes

Capture concise test output summaries and any relevant diffs for the controller split and testing updates.

## Interfaces and Dependencies

Controllers depend on `CodeWorkspaceStore` and the DTOs in `src/SilkHat.Code.Core`. Tests will use the existing test projects in `/tests`. Avoid introducing infrastructure dependencies in tests; use in-memory or temporary resources where needed.

Plan update (2026-01-05 13:59Z): Marked the code analysis controller split as complete after implementing the new controllers.
Plan update (2026-01-05 14:25Z): Recorded test and Docker verification, plus OpenApi and nginx fixes discovered during verification.
Plan update (2026-01-05 14:50Z): Recorded test suite expansion, store interfaces, SymbolKey fallback, and updated verification results.
