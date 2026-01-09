# M13: EF entity configurations + DocID symbol details + thin controllers

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root.

## Purpose / Big Picture

Define a consistent EF Core entity base and configuration layer, then expose DocID-based symbol detail endpoints that return rich symbol metadata (including location) while keeping controllers thin and moving logic into services. The user should be able to query detailed information about types and members by DocID and have those endpoints tested, while persistence remains cleanly configured through IEntityTypeConfiguration classes.

## Progress

- [x] (2026-01-09 16:45Z) Create BaseEntity, update entities to inherit it, and move EF configuration into IEntityTypeConfiguration classes with timestamp updates handled in DbContext.
- [x] (2026-01-09 16:45Z) Add DocID symbol description DTOs/services/controllers for all supported symbol kinds and refactor mermaid generation into a service.
- [x] (2026-01-09 16:56Z) Add/adjust tests for new services/controllers and run validation (dotnet test + docker compose) with containers stopped afterward.

## Surprises & Discoveries

- `dotnet test` failed after the migration because `Npgsql.EntityFrameworkCore.PostgreSQL` was not referenced by `SilkHat.Infrastructure`, which is required for the migration snapshot to compile. Added the package and removed the SQLite dependency.

## Decision Log

- Decision: Use DocID as the primary identifier for symbol detail endpoints with SymbolKey only as a fallback for unsupported symbols.
  Rationale: DocIDs are stable across compilations and already in use for call stack resolution; this keeps API behavior consistent and durable across runs.
  Date/Author: 2026-01-07 / Codex

## Outcomes & Retrospective

- **Delivered:** BaseEntity + configuration classes, DocID symbol description endpoints/services for types/members/namespaces, and Mermaid generation moved into a dedicated service to keep controllers thin.
- **Testing:** `dotnet clean` and `dotnet test` pass; docker compose build/run validated with REPO_ROOT set.
- **Follow-up:** If DocID routing needs further hardening, consider explicit client-side URL encoding helpers and API doc updates.
- **Status:** Milestone complete.

## Context and Orientation

Entity persistence lives in `src/SilkHat.Infrastructure`. Current entities (`RepositoryGroup`, `RepositoryConfig`, `RepositorySolutionConfig`, `MethodImplementationDecision`) embed configuration in `src/SilkHat.Infrastructure/SilkHatDbContext.cs`. Created/Updated timestamps are updated in `SaveChanges` for each entity type, and `RepositorySolutionConfig` does not currently carry Created/Updated fields.

Symbol analysis services live in `src/SilkHat.Code.Analysis`. The DocID helper is `src/SilkHat.Code.Analysis/Services/DocumentationIdUtility.cs`, and `CodeSymbolOutlineService` builds a symbol tree with DocIDs and SymbolKeys. The API layer uses controllers in `src/SilkHat.Api/Controllers`, and call stack endpoints currently generate Mermaid directly in `MethodCallStackController`, which violates the “thin controller” rule. No dedicated symbol description endpoints exist yet.

## Plan of Work

First, introduce a `BaseEntity` in `src/SilkHat.Infrastructure/Entities` that defines `Id`, `CreatedUtc`, and `UpdatedUtc`. Update each entity to inherit from it and add the missing timestamps to `RepositorySolutionConfig`. Create `IEntityTypeConfiguration<T>` classes for each entity in `src/SilkHat.Infrastructure/Configurations` and move all mapping logic from `SilkHatDbContext.OnModelCreating` into these classes. Update `SilkHatDbContext` to apply configurations via `ApplyConfigurationsFromAssembly` and update timestamps for any tracked `BaseEntity` on add/update (Created + Updated on add, only Updated on modify). Generate and apply a migration to add missing columns and verify the schema.

Second, add DocID-based symbol description DTOs in `src/SilkHat.Code.Core/Dtos` for each symbol kind we can locate by DocID (named types, methods, properties, fields, events, namespaces if supported). These DTOs should include symbol name, DocID, SymbolKind, type kind (e.g., class/interface/record), modifiers (static/abstract/virtual/readonly/async), parameters (names, types, ref/out/in), return type for methods, and `CodeLocationDto` for definition locations. Create a `ISymbolDescriptionService` (new interface in `src/SilkHat.Code.Analysis/Abstractions`) and a `SymbolDescriptionService` implementation in `src/SilkHat.Code.Analysis/Services` that resolves a DocID to an `ISymbol` and maps it to the appropriate DTO. Extend `DocumentationIdUtility` with helpers to locate fields and namespaces, or return a clear “not supported” result when DocID cannot be resolved.

Third, add thin controllers for symbol detail endpoints in `src/SilkHat.Api/Controllers`. Use a single controller with distinct endpoints per symbol kind (e.g., `/types/describe`, `/methods/describe`, `/properties/describe`, `/fields/describe`, `/events/describe`, `/namespaces/describe`), or separate controllers per kind if that keeps code simpler. Each endpoint should accept DocID (query or body) and return a ProblemDetails response when not found. Controllers should only resolve workspace/solution, invoke the service, and map results to HTTP responses.

Fourth, refactor Mermaid generation into a service. Introduce `IMethodCallStackMermaidService` in `src/SilkHat.Code.Analysis/Abstractions` with an implementation in `src/SilkHat.Code.Analysis/Services` that takes the call stack node list and returns a Mermaid string. Update `MethodCallStackController` to call the service instead of inlining Mermaid generation. Update or add tests covering Mermaid output generation in service-level tests.

Finally, update tests in `tests/SilkHat.Tests` for the new symbol description service and in `tests/SilkHat.Api.Tests` for the new controllers. Keep existing test intent unchanged. Add controller tests verifying dependency calls and responses per the testing standard. Run `dotnet clean` + `dotnet test` and `docker compose up --build` (stop containers afterwards), and update milestone commit notes.

## Validation and Acceptance

- `dotnet clean` and `dotnet test` succeed without warnings from new code.
- Docker compose builds and runs API + UI; `/api/health` is reachable from the UI container setup.
- New symbol description endpoints return expected DTOs for DocID inputs and return ProblemDetails for unknown DocIDs.
- Mermaid endpoint still returns valid Mermaid and is now generated by a service, not controller logic.
- Entity tables reflect BaseEntity timestamps and configuration via IEntityTypeConfiguration classes.

## Idempotence and Recovery

Entity configuration changes are additive and safe to re-run with migrations. If a migration fails, re-run the migration command after reverting partial changes in the migration file or regenerate a clean migration.

## Interfaces and Dependencies

- `BaseEntity` in `src/SilkHat.Infrastructure/Entities/BaseEntity.cs` with `Id`, `CreatedUtc`, `UpdatedUtc`.
- IEntityTypeConfiguration classes for each entity in `src/SilkHat.Infrastructure/Configurations`.
- `ISymbolDescriptionService` in `src/SilkHat.Code.Analysis/Abstractions` and `SymbolDescriptionService` in `src/SilkHat.Code.Analysis/Services`.
- `IMethodCallStackMermaidService` in `src/SilkHat.Code.Analysis/Abstractions` and `MethodCallStackMermaidService` in `src/SilkHat.Code.Analysis/Services`.
- New DTOs in `src/SilkHat.Code.Core/Dtos` for symbol description outputs.

## Test Plan

Add unit tests in `tests/SilkHat.Tests` covering:
- DocID resolution for each supported symbol kind using the sample solution.
- SymbolDescriptionService mapping for key fields (name, DocID, modifiers, parameters, location).
- Mermaid service output matches expected Mermaid structure for a sample call stack.

Add API controller tests in `tests/SilkHat.Api.Tests` covering:
- Each symbol description endpoint returns 200 with expected payload when provided a valid DocID.
- Each endpoint returns ProblemDetails when DocID is missing or cannot be resolved.
- Mermaid controller uses the service and returns the Mermaid output for known call stack inputs.

Run `dotnet clean` and `dotnet test` in the repo root. Run `docker compose up --build` and verify API + UI, then `docker compose down`.

Plan Update Notes: 2026-01-09 — Marked progress complete and recorded surprises/outcomes after implementing the EF configuration changes, DocID symbol description endpoints, and Mermaid service refactor.
