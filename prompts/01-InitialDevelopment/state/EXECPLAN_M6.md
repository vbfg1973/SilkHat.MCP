# M6 Hardening + Docs + Scripts

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this milestone, the project has a documented, repeatable way to build, test, and run locally and via Docker, along with concise architecture and troubleshooting docs. The system remains fully functional with stable tests, and performance-sensitive operations (repository loading and code queries) are hardened with caches where appropriate. A new contributor can follow scripts and docs to validate the app end-to-end without relying on tribal knowledge.

## Progress

- [x] (2026-01-07 08:41Z) Reviewed milestone requirements and current repository state to scope M6 updates.
- [x] (2026-01-07 08:43Z) Audited existing docs/scripts: only docs/README.md exists and no scripts directory.
- [x] (2026-01-07 08:45Z) Added build/test/run and Docker helper scripts under /scripts.
- [x] (2026-01-07 08:45Z) Updated documentation: README plus architecture, API notes, operations, and troubleshooting docs.
- [x] (2026-01-07 08:46Z) Completed performance review; existing in-memory workspace caches and tree maps cover LoadedRepository indices without new changes.
- [ ] Run `dotnet test` and confirm all non-infrastructure tests pass (attempted; MSBuild named pipe permission denied).
- [ ] Run `docker compose up --build` and confirm API + UI run in Docker (attempted; Docker daemon socket permission denied).
- [x] (2026-01-07 08:47Z) Produced Milestone Commit Notes for M6.

## Surprises & Discoveries

- Observation: LoadedRepository indexing already caches code workspaces and tree children maps per solution.
  Evidence: `CodeWorkspaceStore` holds `CodeRepositoryWorkspace` per repository and `CodeSolutionWorkspace.TreeChildrenByParent` precomputes child lookups.
- Observation: `dotnet test` fails in this environment due to MSBuild named pipe permissions.
  Evidence: `System.Net.Sockets.SocketException (13): Permission denied` while binding MSBuild pipe.
- Observation: Docker compose fails in this environment due to Docker socket permissions.
  Evidence: `permission denied while trying to connect to the Docker daemon socket`.

## Decision Log

- Decision: No additional performance caches added in M6.
  Rationale: Existing workspace stores and tree child maps already provide fast repeated access per LoadedRepository, and git caches are already in place.
  Date/Author: 2026-01-07 08:46Z / Codex

## Outcomes & Retrospective

Docs and scripts are now in place, and the performance review indicates existing caches are sufficient. Validation is blocked in this environment by MSBuild pipe and Docker socket permissions.

## Context and Orientation

The solution lives at `/SilkHat.sln` with source under `/src` and tests under `/tests`. Docker compose lives at `/docker-compose.yml`. Prior milestones introduced solution-scoped code analysis, repository discovery, PostgreSQL persistence, and IDE file tabs. The app currently runs via Docker with API and UI containers, and testing uses xUnit with bUnit for UI tests. This milestone focuses on hardening: scripts, docs, and performance tuning without changing existing behavior.

## Plan of Work

First, inventory current scripts and documentation in `/docs` and any repo-level scripts. Note gaps relative to the required docs: architecture overview, API notes, operations, and troubleshooting. Then add minimal scripts (shell or .NET tools) that encapsulate build/test/run and Docker usage. Next, update documentation to reflect actual runtime configuration (PostgreSQL, REPO_ROOT, port mappings) and the new solution-scoped code endpoints. Finally, perform a performance pass: identify hotspot queries or repeated computations in the repository load pipeline and code analysis endpoints, and add cache layers scoped to `LoadedRepository` with clear invalidation on repository reload. Keep changes minimal and covered by tests where possible.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build -d

## Validation and Acceptance

- Scripts exist to build, test, and run the system locally and via Docker.
- Documentation covers architecture, API notes, operations, and troubleshooting.
- Performance improvements are observable via faster repeated calls (or a documented metric if timing is not trivial).
- `dotnet test` passes and Docker services run successfully.

## Idempotence and Recovery

Scripts should be safe to run multiple times. Caches must be invalidated on repository reload to avoid stale data. Documentation changes are additive and can be revised without breaking code.

## Artifacts and Notes

Capture example outputs for scripts and any performance evidence (timings or notes about cache hits) as short excerpts in this plan.

## Interfaces and Dependencies

Any new scripts should live at the repo root or `/scripts` and should not require external dependencies beyond `dotnet` and `docker`. Performance caches should be implemented in the existing repository/analysis services and be keyed by repository config or solution ID where appropriate.

## Test Plan

- Run `dotnet test` and ensure all unit/controller/UI tests pass.
- If new caches are added, update or add tests that verify cache hits/misses and invalidation on reload.
- Run Docker compose and verify the UI reaches API health endpoints.
