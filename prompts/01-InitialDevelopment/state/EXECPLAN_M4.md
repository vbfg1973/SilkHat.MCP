# M4 Code Loading with Buildalyzer + Roslyn In-Memory Host

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

This milestone loads repository solutions into an in-memory Roslyn workspace using Buildalyzer and exposes code navigation endpoints. After completion, the API can list projects, show references, search namespaces and named types, and perform symbol lookups by SymbolKey while the UI presents an IDE-like scaffold.

## Progress

- [x] (2026-01-05 13:35Z) Implemented Buildalyzer-based solution loading and in-memory Roslyn workspace per loaded repository.
- [x] (2026-01-05 13:35Z) Built indices for projects, references, namespaces, named types, and symbol lookup by SymbolKey.
- [x] (2026-01-05 13:35Z) Exposed code navigation endpoints under `/api/repositories/{id}/code/...` via controllers.
- [x] (2026-01-05 13:35Z) Added IDE-like UI scaffold with left tree, center tabs viewer, and right placeholder panel.
- [x] (2026-01-05 13:49Z) Ran `dotnet test` successfully.
- [x] (2026-01-05 13:49Z) Ran `docker compose up --build` successfully after Buildalyzer and SymbolKey fixes.
- [x] (2026-01-05 13:50Z) Updated milestone state and commit notes.

## Surprises & Discoveries

- Observation: Buildalyzer.Workspaces can add projects into an AdhocWorkspace without manual MSBuildWorkspace setup.
  Evidence: `AnalyzerManager(...).Projects.Values` are added via `AddToWorkspace(...)`.
- Observation: Roslyn SymbolKey APIs are internal in the Workspaces assembly and required reflection to access.
  Evidence: Compilation failed with `SymbolKey`/`SymbolKeyExtensions` inaccessible due to protection level.

## Decision Log

- Decision: Use reflection to access SymbolKey helper methods and keep SymbolKey strings stable for lookup.
  Rationale: SymbolKey APIs are internal in the Workspaces assembly but still required by the milestone.
  Date/Author: 2026-01-05 / Codex

## Outcomes & Retrospective

M4 delivers code analysis endpoints backed by Buildalyzer/Roslyn and an IDE-like UI scaffold.

## Context and Orientation

Repository load orchestration and streaming are in `src/SilkHat.Analysis`, with `LoadedRepositoryStore` tracking loaded repositories. Controllers live under `src/SilkHat.Api/Controllers`. M4 adds code analysis functionality under `src/SilkHat.Code.Analysis` and DTOs under `src/SilkHat.Code.Core`. The UI lives in `src/SilkHat.Ui`, and the IDE-like layout will extend the dashboard.

## Plan of Work

Add Buildalyzer and Roslyn workspace packages, then implement a code analysis service that loads one or more solutions per repository root (defaulting to detected `.sln` files). Build indices for projects, references, namespaces, named types, and symbol lookup by SymbolKey without returning Roslyn symbols directly. Extend the loaded repository model to hold analysis results. Implement API endpoints for code navigation. Update the UI with a scaffolded IDE-like view (left tree, center tabs, right panel) and wire it to the new endpoints. Validate with tests and Docker.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build

## Validation and Acceptance

Run `dotnet test` and expect all tests to pass. Run `docker compose up --build`, load a repository, and verify:
- `GET /api/repositories/{id}/code/projects` lists projects.
- `GET /api/repositories/{id}/code/projects/{projectKey}/references` and `referenced-by` return data.
- `GET /api/repositories/{id}/code/namespaces?prefix=...` returns matching namespaces.
- `GET /api/repositories/{id}/code/named-types?...` returns filtered named types.
- `POST /api/repositories/{id}/code/symbols/lookup` resolves a SymbolKey with expected kind.

The UI shows the IDE-like scaffold.

## Idempotence and Recovery

Loading is repeatable. If analysis fails, unload and reload the repository to recreate the workspace. Indices are rebuilt on load.

## Artifacts and Notes

Example namespace query response:

    GET /api/repositories/{id}/code/namespaces?prefix=System
    ["System","System.Collections","System.IO"]

## Interfaces and Dependencies

Buildalyzer and Roslyn workspaces are used in `src/SilkHat.Code.Analysis`. Controllers must return DTOs in `src/SilkHat.Code.Core` rather than Roslyn symbols. Symbol lookup uses `Microsoft.CodeAnalysis.SymbolKey` strings with an expected kind to validate the result.
