# Milestones (Hard Gates)

Each milestone must end with:
- API runs in Docker
- UI runs in Docker (nginx serving published WASM)
- dotnet test passes
- A Milestone Commit Notes message is produced
- State files updated

## M1: Skeleton + Dockerized Hello World
- Create solution + project skeleton under /src and /tests
- API: minimal endpoint /health
- UI: MudBlazor template with theme toggle + calls /health
- docker-compose: api + ui containers working
- Swagger enabled
- Minimal README in /docs

## M2: Persistence for Repository Configs + Groups
- EF Core SQLite in SilkHat.Infrastructure
- CRUD for RepositoryConfig and RepositoryGroup via API
- UI: basic dashboard to list/create/edit configs and groups
- Persisted DB file via docker volume

## M2a: Controller-Based API Endpoints
- Convert all existing API endpoints to controller-based endpoints
- Ensure ProblemDetails and correlationId behavior are preserved
- Wire future endpoints to use controllers only
- Swagger reflects controller endpoints

## M3: Command/Channel Streaming + Repository Load Orchestration
- Implement command processor per repository
- Implement streaming NDJSON endpoint for /load
- LoadedRepository runtime model created (workspace placeholder ok if not compiling yet)
- UI: show load progress stream

## M4: Code Loading with Buildalyzer + Roslyn In-Memory Host
- Load default solutions, build workspace, create compilations
- Endpoints:
  - list projects (filter by name)
  - project references / referenced-by
  - namespaces prefix search
  - named types list + filtering
  - symbol lookup by SymbolKey with expected kind
- UI: IDE-like view scaffold: left tree, center tabs file viewer (simple text ok), right placeholder

## M4a: Testing Standardization
- Add controller endpoint tests that verify dependencies are called and responses are correct.
- Add service unit tests that verify dependency interactions, expected responses, and edge cases.
- Add service integration tests to validate real behavior.
- Establish the testing standard as a requirement for all future controllers and services.

## M4b: UI Hosting Fixes
- Resolve UI routing/hosting issues that prevent loading the SPA from Docker.
- Ensure UI reaches the API on the configured host ports.
- Verify the UI loads from the mapped host port and can call `/api/health`.
- Establish a UI component testing strategy and add initial bUnit coverage for existing components.

## M5: Git via Process: Tree Listing + File History + Co-change
- Implement git query layer using Process
- Endpoints:
  - tree listing with filters (name/type/changedAfter/author)
  - file history
  - co-change stats
- UI: file explorer uses git tree + shows file history panel (right pane)

## M5a: Git History/Co-change Enhancements + Route Fixes
- Extend git DTOs to capture total change counts and line deltas per file change.
- Update co-change stats to include total change count for the target file.
- Update file history change records to include before/after line counts and a categorical change kind.
- Adjust git file history/co-change routes to `/git/files/{path}/history` and `/git/files/{path}/cochanges` with URI-encoded paths.

## M5b: Repository Root Discovery + Group Loads
- Add REPO_ROOT environment variable in docker compose and mount it at `/repos` in the API container.
- List repository folders quickly from REPO_ROOT and only allow adding repositories that are verified as git repos.
- Load multiple repositories at once via repository groups.
- Restrict IDE screens to loaded repositories only.

## M5c: Repository Group Loader UI Overhaul
- Replace existing repository config/group UI with a new component focused on repository groups and load orchestration.
- Create repository groups and add repositories from the available list, selecting solution files to load per repository.
- Edit existing groups: add/remove repositories and enable/disable individual solutions for loading.
- Loading a repository group destroys existing workspaces and reloads fresh compilations for all enabled solutions in the group.

## M5d: Code Analysis Without Buildalyzer
- Replace Buildalyzer-based loading with project/solution parsing and MSBuild-free analysis.
- Parse `.sln` and `.csproj` files directly to build:
  - project reference graphs
  - package references + versions
  - compile items and constants needed for analysis
- Use Roslyn with parsed inputs (no external tools) to create compilations and indexes.
- Preserve existing API contracts for code analysis endpoints (projects, namespaces, named types, symbol lookup).
- Provide an IDE tree endpoint derived from solution projects (projects as roots with folders/files beneath).

## M5e: PostgreSQL Migration + Docker Compose Refinement
- Migrate persistence from SQLite to the latest PostgreSQL.
- Update docker compose to run PostgreSQL alongside API/UI.
- Ensure database configuration is driven by environment variables and supports local development.
- Provide migration path and maintain data access via standard database tools.
- Add a dedicated integration test project using Testcontainers for real-database tests, excluded from default test runs.
- Add pgAdmin in docker compose, preconfigured to connect to the PostgreSQL service.

## M5f: Solution-Scoped Code Endpoints
- Introduce stable solution identifiers (prefer reading from the solution when possible).
- Update all code-related endpoints to include solutionId in the route.
- Ensure code tree, projects, namespaces, named types, symbol lookup, and any other solution-derived data are scoped to a specific solution.
- Update UI/API clients and tests to use solution-scoped routes.

## M5g: IDE File Tabs + Fast File Content
- When selecting a file in the IDE tree, open the file content in a new tab in the center panel.
- Remove the welcome tab and allow the tab area to grow to fit file content.
- Serve file contents directly from the repository filesystem using repository + solution context for fast lookup.
- Add API/UI tests for file content retrieval and tab behavior.

## M6: Hardening + Docs + Scripts
- Scripts: build/test/run + docker convenience scripts
- Docs: architecture overview, API notes, troubleshooting
- Performance pass: caching indices per LoadedRepository
- Ensure all tests stable

## M7: Git Commit Query + Integration Coverage
- Add commit query endpoints and models for author, sha, date range, merge vs ordinary commits, and file change filters.
- Support change type enums for file changes (add/modify/delete/rename/etc).
- Add unit tests that parse commit metadata and file change details (including the provided commit examples).
- Add integration tests that query the current repository and assert the example commits appear in expected results.
- Add generic paging (pageNumber/pageSize) across all list-returning endpoints, with shared result models and updated clients/tests.
- IDE file tabs show last commit metadata (author + ISO date) and a diff toggle that annotates added (green) and deleted (red) lines from the last commit.

At the end of each milestone, STOP and emit Milestone Commit Notes.
