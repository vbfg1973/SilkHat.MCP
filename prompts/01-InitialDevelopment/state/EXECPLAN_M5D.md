# M5d Code Analysis Without Buildalyzer

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this change, the API will load repository code for analysis without Buildalyzer or any external build tools, and the IDE tree view will be built from solution data rather than git. A user can load a repository group and still see projects, namespaces, named types, and symbol lookups in the same API endpoints as before, while the IDE tree shows projects as roots with folders and files beneath. The change is visible by loading a repository group and observing that the analysis endpoints respond with project and code data even when Buildalyzer is removed and the IDE tree displays project-based entries.

## Progress

- [x] (2026-01-06 20:12Z) Capture the current analysis data needs from the API endpoints and the workspace model.
- [x] (2026-01-06 20:12Z) Define the data contracts for solution and project parsing and confirm how they map to analysis outputs.
- [x] (2026-01-06 20:32Z) Implement a solution parser that reads `.sln` files and resolves project paths.
- [x] (2026-01-06 20:38Z) Implement a project parser that reads `.csproj` files and collects project references, package references, compile items, and constants.
- [x] (2026-01-06 20:48Z) Replace Buildalyzer in the code workspace loader with the new parsing pipeline and Roslyn compilation setup.
- [x] (2026-01-06 20:52Z) Update and add tests using `samples/solution01` to validate parsing, project graphs, and code analysis outputs.
- [x] (2026-01-06 22:10Z) Add code tree DTOs and compute project-based tree entries from loaded solutions.
- [x] (2026-01-06 22:15Z) Add `/api/repositories/{id}/code/tree` endpoint and update the IDE UI to use it.
- [x] (2026-01-06 22:20Z) Extend controller and UI tests to cover the code-based tree behavior.
- [ ] Run `dotnet test` and confirm all non-infrastructure tests pass.
- [ ] Run `docker compose up --build` and confirm API + UI run in Docker.
- [x] (2026-01-06 21:04Z) Write Milestone Commit Notes for M5d and update milestone state files.

## Surprises & Discoveries

- Observation: `dotnet test` fails in this environment with MSBuild named pipe permission errors (`SocketException (13): Permission denied`).
  Evidence: MSBuild reports MSB1025 when trying to create the out-of-proc node pipe.
- Observation: `docker compose up --build` fails locally because the Docker daemon socket is not accessible from this environment.
  Evidence: `dial unix /var/run/docker.sock: connect: operation not permitted`.

## Decision Log

- Decision: Avoid Buildalyzer and MSBuild; parse solution and project files directly.
  Rationale: Containerized analysis must not depend on external build tooling to evaluate projects.
  Date/Author: 2026-01-06 / Codex
- Decision: Build Roslyn projects from parsed `.csproj` inputs using AdhocWorkspace with default runtime metadata references.
  Rationale: Roslyn needs compilations for symbol keys and type indexes, but we can assemble them without MSBuild by loading source files and runtime assemblies.
  Date/Author: 2026-01-06 / Codex
- Decision: Align Roslyn workspace package version with EF Core Design to avoid NU1107 conflicts during container restore.
  Rationale: `Microsoft.EntityFrameworkCore.Design` pulls `Microsoft.CodeAnalysis.CSharp.Workspaces` 4.8.0, so we match the workspace package to 4.8.0 for compatibility.
  Date/Author: 2026-01-06 / Codex
- Decision: Add `Microsoft.CodeAnalysis.CSharp` package to the analysis project.
  Rationale: The new loader uses `CSharpParseOptions` directly, which requires the explicit C# Roslyn package for compilation in the container build.
  Date/Author: 2026-01-06 / Codex
- Decision: Add `Microsoft.Extensions.Options` to SilkHat.Analysis for container builds.
  Rationale: Repository discovery uses `IOptions<>` and the analysis project must reference the options package explicitly.
  Date/Author: 2026-01-06 / Codex
- Decision: Add `Microsoft.CodeAnalysis.CSharp.Workspaces` to support C# language services in AdhocWorkspace.
  Rationale: The Roslyn workspace needs C# workspace services registered, otherwise it throws "The language 'C#' is not supported."
  Date/Author: 2026-01-06 / Codex
- Decision: Install git in the API runtime container image.
  Rationale: Git CLI is required at runtime for tree/history/co-change endpoints, and the base aspnet image does not include it.
  Date/Author: 2026-01-06 / Codex
- Decision: Serve the IDE tree from solution-based project data instead of git.
  Rationale: The IDE should show projects as roots with their files and folders; git tree is not aligned with project boundaries.
  Date/Author: 2026-01-06 / Codex

## Outcomes & Retrospective

TBD.

## Context and Orientation

Code analysis currently relies on Buildalyzer in `src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs`. That loader produces `CodeRepositoryWorkspace` objects defined in `src/SilkHat.Code.Analysis/Models/CodeRepositoryWorkspace.cs`. API endpoints in `src/SilkHat.Api/Controllers` read the workspace data and expose projects, namespaces, named types, and symbol lookups. The new implementation must keep the same outward behavior, but must gather inputs by reading `.sln` and `.csproj` files directly in C# without invoking `dotnet`, MSBuild, or any other external tool. The IDE tree must be derived from the loaded solution so that projects are the roots and files/folders descend under the project they belong to. “Project graph” means the list of projects in a solution and the project-to-project references declared in `.csproj` files.

## Plan of Work

First, enumerate what the analysis endpoints require. Inspect the current loader and the controller mappings to understand which pieces of data must be preserved. Second, implement a solution parser that reads `.sln` files, extracts project entries, and resolves those paths relative to the solution directory. Third, implement a project parser that reads `.csproj` XML and extracts `ProjectReference`, `PackageReference`, `Compile` items, and property values such as `DefineConstants` and `TargetFramework`. Fourth, update the loader to construct Roslyn compilations from the parsed project data and populate `CodeRepositoryWorkspace` so the existing endpoints keep working and the project-based tree can be produced. Fifth, add a code tree endpoint and update the UI to consume it, ensuring file history uses repo-relative paths from the tree entries. Sixth, extend tests using `samples/solution01` to verify parsing correctness, project graphs, named types, namespaces, symbol keys, and tree output. Finally, run tests and Docker and record results.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build -d

## Validation and Acceptance

Run `dotnet test` and expect all tests to pass. Start Docker and verify:

- The API responds to project list, namespaces, named types, symbol lookup, and code tree endpoints for a loaded repository group.
- Repository group load succeeds without Buildalyzer and produces code analysis output.

## Idempotence and Recovery

Parsing `.sln` and `.csproj` files is read-only and safe to repeat. If parsing fails, surface clear errors and allow reattempts after file fixes. If Docker fails, rerun `docker compose up --build -d` after correcting the error.

## Artifacts and Notes

Record any parsing edge cases and the files they were observed in so future projects can be handled consistently.

## Interfaces and Dependencies

Maintain `CodeRepositoryWorkspace` output shape and DTOs in `src/SilkHat.Code.Core`. Introduce new parser types under `src/SilkHat.Code.Analysis` (for example, `SolutionParser` and `ProjectParser`), and update `CodeWorkspaceLoader` to depend on them. Do not call `dotnet`, MSBuild, or other external tools; all parsing must happen in-process via file reads and XML parsing.

## Test Plan

Add or expand tests under `tests/SilkHat.Tests` using `samples/solution01` to verify:

- Solution parsing returns both projects and resolved paths.
- Project parsing captures project references, package references, and compile items.
- Roslyn compilations are created and named types/namespaces are indexed.
- Code tree entries include project roots and file paths derived from loaded solutions.
- API controller tests cover the code tree endpoint and UI tests cover rendering the new tree data.

Run `dotnet test` and confirm the new tests pass.

Plan update (2026-01-06 20:12Z): Created M5d ExecPlan for Buildalyzer replacement and project/solution parsing.
Plan update (2026-01-06 20:52Z): Implemented parser classes, replaced Buildalyzer loader, and added parsing tests; remaining work is running tests, Docker verification, and milestone notes.
Plan update (2026-01-06 21:02Z): Attempted `dotnet test` and Docker verification; both blocked by environment permissions and recorded in Surprises & Discoveries.
Plan update (2026-01-06 21:12Z): Adjusted Roslyn workspace package version to 4.8.0 to resolve container restore conflicts.
Plan update (2026-01-06 21:20Z): Added Microsoft.CodeAnalysis.CSharp package to resolve missing CSharpParseOptions build error.
Plan update (2026-01-06 21:26Z): Added Microsoft.Extensions.Options to fix missing IOptions<> build errors during publish.
Plan update (2026-01-06 21:32Z): Added Microsoft.CodeAnalysis.CSharp.Workspaces to fix C# language services missing in AdhocWorkspace.
Plan update (2026-01-06 21:40Z): Added git installation to API Dockerfile to support runtime git CLI usage.
Plan update (2026-01-06 21:55Z): Expanded scope to include project-based code tree endpoint and IDE UI updates.
