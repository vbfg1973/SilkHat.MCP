# M7 Git Commit Query + Integration Coverage

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this milestone, the system can query git commits by author, SHA (full or shortened), date ranges, merge vs ordinary commits, and file change filters (including rename/delete). The API and services expose explicit change-type enums for file changes. Tests cover parsing commit metadata and changes using known commit examples, and integration tests verify end-to-end git queries against this repository. Generic paging is available across all list-returning endpoints with sensible defaults for page number and page size, and the API clients and tests are updated accordingly. The IDE file tabs show last-commit metadata for the open file and can optionally render a line-by-line diff overlay (added lines in green, deleted lines in red), with a reusable line-based annotation mechanism that can later support Roslyn location highlighting.

## Progress

- [x] (2026-01-07 09:05Z) Defined the milestone scope and captured required commit examples for tests.
- [x] (2026-01-08 13:33Z) Inspected git CLI parsing and extended git commit query models and parsing logic.
- [x] (2026-01-08 13:33Z) Added commit DTOs and file change modeling for commit query results.
- [x] (2026-01-08 13:33Z) Implemented git commit query methods for author/sha/date/merge/ordinary/file-change filters.
- [x] (2026-01-08 13:33Z) Added unit tests that parse the provided commit examples into models.
- [x] (2026-01-08 13:33Z) Added integration tests that query this repository and assert the example commits are returned.
- [x] (2026-01-08 14:18Z) Defined paging query/result models, defaults, and applied paging to git commit queries.
- [x] (2026-01-08 14:18Z) Applied paging to all list-returning endpoints (API, clients, and tests).
- [x] (2026-01-08 15:08Z) Implemented git last-change + diff endpoint and UI diff toggle in IDE file tabs.
- [x] (2026-01-08 15:08Z) Added unit/controller/UI tests for last-change metadata and diff rendering.
- [x] (2026-01-08 15:14Z) Added explicit UI rerender after opening tabs and toggling diff to ensure test-visible markup updates.
- [x] (2026-01-08 15:23Z) Expanded last-commit metadata to include abbreviated SHA and author email, and rendered commit details in a MudCard with diff toggle.
- [x] (2026-01-08 15:28Z) Confirmed `dotnet test` passes (user-verified).
- [x] (2026-01-08 15:28Z) Confirmed API/UI functionality end-to-end for the milestone (user-verified).
- [x] (2026-01-08 15:28Z) Confirmed docker compose up --build runs API + UI for the milestone (user-verified).
- [ ] (2026-01-08 14:48Z) Run `docker compose up --build` and confirm API + UI run in Docker (blocked: docker socket permission denied; also requires REPO_ROOT env).
- [ ] Produce Milestone Commit Notes for M7.

## Surprises & Discoveries

- Observation: `dotnet test` fails in this environment due to MSBuild named pipe permissions.
  Evidence: `MSBUILD : error MSB1025: An internal failure occurred while running MSBuild. System.Net.Sockets.SocketException (13): Permission denied` while creating `NamedPipeServerStream`.
- Observation: With `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet test -m:1`, compilation succeeds but VSTest cannot open its socket server.
  Evidence: `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start` and test run aborts.
- Observation: `docker compose up --build` requires `REPO_ROOT` and docker socket access in this environment.
  Evidence: `required variable REPO_ROOT is missing a value` then `permission denied while trying to connect to the Docker daemon socket at unix:///var/run/docker.sock`.

## Decision Log

- Decision: Use a git log format with explicit COMMIT/BODY markers to parse headers, body, and change status lines reliably.
  Rationale: The markers allow parsing multi-line commit bodies without confusing them with file change entries.
  Date/Author: 2026-01-08 13:33Z / Codex

- Decision: Add a shared paging contract (query + result) and apply it consistently to all list endpoints.
  Rationale: Consistent paging reduces payload size and improves UX; central defaults minimize boilerplate.
  Date/Author: 2026-01-08 13:33Z / Codex

- Decision: Apply paging in controllers and service layers with PagedResult while keeping existing DTO shapes stable where possible.
  Rationale: Central paging logic avoids duplicating query logic and keeps list endpoints consistent without forcing large client rewrites.
  Date/Author: 2026-01-08 14:18Z / Codex

- Decision: Use `git log -n 1 --follow` for last-change metadata and `git show --unified=0` to build line-level diff annotations.
  Rationale: These commands avoid external tools, yield stable output for parsing, and provide precise insertion points for added/deleted lines.
  Date/Author: 2026-01-08 15:08Z / Codex

- Decision: Extend last-change metadata with abbreviated SHA and author email, and render commit details in a MudCard alongside the diff toggle.
  Rationale: The UI needs a compact, structured commit summary with specific fields and a consistent control location.
  Date/Author: 2026-01-08 15:23Z / Codex

## Outcomes & Retrospective

M7 delivers paged list endpoints, git commit query capability with change-type modeling, and IDE file tabs that show last-commit metadata plus an optional diff overlay. Parsing and integration tests cover commit metadata and file changes (including the supplied commit examples), and the UI now displays commit details in a structured MudCard with a diff toggle. Remaining gaps are limited to future UX polish (syntax highlighting or Roslyn-based highlighting) and any follow-on git feature work beyond the scope of this milestone.

## Context and Orientation

Git operations live under `src/SilkHat.Git.Analysis` and are implemented via git CLI calls parsed into DTOs in `src/SilkHat.Git.Core`. Existing endpoints cover tree listing, file history, and co-change stats. This milestone expands commit queries and requires clear modeling of file change types (add/modify/delete/rename/etc), along with unit and integration tests. The integration tests should operate on this repository as a known git repo and validate the supplied commit examples.

## Plan of Work

First, inspect current git parsing to determine where to insert commit-query support. Add a commit query service that issues git log commands with appropriate filters (author, sha, date range, merges-only, no-merges, file path with change status). Define a change-type enum in `src/SilkHat.Git.Core` and update parsing to map git status codes to this enum. Then expose these queries via API endpoints (likely under `GET /api/repositories/{id}/git/commits` with query parameters and a separate endpoint for sha lookups). Update UI clients only if needed for test coverage.

Next, define paging models (query + result) in a shared location (likely `src/SilkHat.Core` or the relevant domain) and provide default values (page number 1, page size 50 unless overridden). For git commit queries, use git CLI options such as `--skip` and `--max-count` to avoid loading the full history when possible. Then apply the same paging models across all list-returning endpoints (API controllers, DTOs, and API clients), ensuring existing tests are updated to include paging and that result objects expose total counts where feasible.

Next, add IDE file-tab metadata + diff overlay support. Implement a git API endpoint that returns the latest commit metadata for a file path (author + ISO date + subject) and, when requested, a diff against its parent commit. Parse `git log -n 1 --follow` for the last commit and `git show <sha> --unified=0 --format= -- <path>` for hunks. Map diff hunks to line annotations where additions and deletions can be inserted at the correct line positions in the rendered text. Update the UI file tab header to show author/date on the left and keep the “close all tabs” action on the right. Add a diff toggle checkbox; when enabled, fetch and render annotated lines, and when disabled, render plain content. Store per-tab line annotations so switching between solutions preserves the state. Add tests for the new git diff API and UI rendering/toggle behavior.

Finally, add unit tests that parse the provided commit examples into your models and verify all fields (author, date, message, merge parent SHAs, file change types). Add integration tests that call the git query service against this repo and assert the example commits appear in the result sets for the appropriate queries (author, sha, date range, merge vs ordinary, file change). Run tests and docker compose.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build -d

## Validation and Acceptance

- Commit queries return the expected commit metadata and file change types.
- Provided commit examples are parsed correctly in unit tests.
- Integration tests verify the example commits appear in the correct query results.

## Idempotence and Recovery

Git queries are read-only. If parsing fails, adjust the git log format string and update the parser tests with the corrected format.

## Artifacts and Notes

Include the exact git log format used in parsing and any example outputs needed for troubleshooting.

## Interfaces and Dependencies

Define the change-type enum in `src/SilkHat.Git.Core` and update existing DTOs or add new commit DTOs in the same project. Implement new query methods in `src/SilkHat.Git.Analysis/Services/GitCli.cs` and add new API controllers or controller actions in `src/SilkHat.Api/Controllers` for commit queries. Add paging contracts (query + result) in a shared DTO location and update list-returning endpoints to use them. Tests should live in `tests/SilkHat.Tests` (unit) and `tests/SilkHat.IntegrationTests` (integration).

For the diff overlay, add new DTOs in `src/SilkHat.Git.Core/Dtos` (e.g., `GitFileLastChangeDto`, `GitFileDiffLineDto`, `GitFileDiffDto`) and add matching API models in the UI. Extend `IGitCli` with a method for fetching last-commit metadata plus diff lines. Add an API endpoint in `src/SilkHat.Api/Controllers/GitFilesController.cs` under `/api/repositories/{id}/git/files/{path}/last-change` with an `includeDiff` query parameter. Update `RepositoryApiClient` and `Ide.razor` to fetch and render this data, and add a line-annotation helper that can also be used later for Roslyn-based highlights.

## Test Plan

- Unit tests in `tests/SilkHat.Tests` to validate parsing for author, date range, sha matching, merge/ordinary detection, and file change enums.
- Integration tests in `tests/SilkHat.IntegrationTests` that run git queries against this repository, asserting the provided commit examples appear in results.
- Run `dotnet test` and expect all tests to pass.
- Add controller tests for the new git file last-change + diff endpoint, covering author/date/subject and diff lines.
- Add service tests for diff parsing logic with representative hunks (additions + deletions).
- Add UI tests that verify the header shows author/date, and that the diff toggle switches annotated vs plain content.

Plan update note: Updated progress and decision log to reflect paging implementation and its placement, plus marked paging steps complete (2026-01-08 14:18Z).
Plan update note: Extended M7 to include IDE file-tab last-commit metadata and diff overlay support, with new API endpoint and tests (2026-01-08 15:08Z).
Plan update note: Marked M7 validations complete based on user-confirmed tests and runtime checks, and summarized outcomes (2026-01-08 15:28Z).
