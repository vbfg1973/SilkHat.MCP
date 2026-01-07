# M5g IDE File Tabs + Fast File Content

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with `.agents/PLANS.md`.

## Purpose / Big Picture

After this milestone, clicking a file in the IDE tree opens the file contents in a new tab in the center panel. The center panel no longer shows a welcome tab and instead renders only file tabs, expanding vertically with the tree panel and allowing larger files to display in a scrollable view. File contents are served directly from the repository filesystem using the repository config and solution context so the API returns contents quickly without re-parsing Roslyn structures. The solution selector lists all loaded solutions across repositories and preserves the open tabs and tree expansion state when switching between solutions.

If an easy, built-in component exists to add syntax highlighting, we will use it. If not, we will render plain text in a monospaced container and record the limitation.

## Progress

- [x] (2026-01-06 23:08Z) Reviewed existing IDE tree behavior, code tree DTOs, and solution-scoped routes to determine integration points for file content retrieval.
- [x] (2026-01-06 23:17Z) Defined file content DTOs, service, controller, and validation for file lookup scoped by repository + solution.
- [x] (2026-01-06 23:17Z) Updated IDE UI to open file tabs, remove the welcome tab, and render file contents with proper sizing.
- [x] (2026-01-06 23:17Z) Added controller/service/UI tests for file content retrieval and tab behavior.
- [x] (2026-01-06 23:27Z) Removed file history panel and enabled solution switching across all repositories with per-solution view state retention.
- [x] (2026-01-06 23:36Z) Added closeable tabs, a close-all action, and fixed-width tree layout with expanding tab panel.
- [x] (2026-01-07 08:32Z) Added drawer toggle, fixed tab close behavior, and left-aligned 90% width IDE layout.
- [x] (2026-01-07 08:38Z) Verified tests pass and Docker UI/API run for M5g.
- [ ] Run `dotnet test` and confirm all non-infrastructure tests pass.
- [ ] Run `docker compose up --build` and confirm API + UI run in Docker.

## Surprises & Discoveries

None yet.

## Decision Log

- Decision: Use a dedicated `CodeFilesController` with a `path` query parameter representing the solution display path.
  Rationale: Files are significant enough to justify a dedicated controller, and display paths match the tree UI while mapping cleanly to repository-relative paths via tree entries.
  Date/Author: 2026-01-06 23:17Z / Codex
- Decision: Defer syntax highlighting because MudBlazor does not include a lightweight, built-in code highlighter.
  Rationale: No easy component is available in the current UI stack; we render monospaced text while leaving room for future enhancement.
  Date/Author: 2026-01-06 23:17Z / Codex
- Decision: Preserve IDE tree/tab state per solution when switching across repositories.
  Rationale: Switching solutions should not discard open files or expanded tree state; caching view state per solution keeps the UI consistent.
  Date/Author: 2026-01-06 23:27Z / Codex
- Decision: Use fixed-width tree panel with a flex-grow tab panel to occupy remaining width.
  Rationale: Keeps the repository tree stable while allowing file content to grow with the viewport.
  Date/Author: 2026-01-06 23:36Z / Codex
- Decision: Make the main navigation drawer collapsible via the app bar menu button.
  Rationale: Allows the IDE to reclaim horizontal space on wide screens while keeping navigation accessible.
  Date/Author: 2026-01-07 08:32Z / Codex

## Outcomes & Retrospective

Implemented fast file content loading with per-solution IDE state, closeable tabs, and a responsive layout. Tests and Docker validation now pass, leaving M5g ready for milestone completion.

## Context and Orientation

The IDE screen lives in `src/SilkHat.Ui/Pages/Ide.razor` and currently renders a tree using `MudTreeView` plus a center panel with a placeholder tab. Tree entries come from the code tree endpoint `GET /api/repositories/{id}/code/solutions/{solutionId}/tree`, which returns `CodeTreeEntryDto` records with `RepositoryPath`, `DisplayPath`, `Name`, and `Type`. Repository configs and solutions are persisted in the database via `RepositoryConfig` and `RepositorySolutionConfig` entities, and code workspaces are loaded in `src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs`.

File content should be retrieved directly from the repository filesystem for speed. We already have the repository root path on `RepositoryConfig.RootPath`, and each tree entry includes a repository-relative path (`RepositoryPath`) that is normalized as `./path/from/root`. The new API endpoint should combine the repository root with the repository-relative path, validate it does not escape the repository root, then return the file contents.

## Plan of Work

First, define a file content service in `src/SilkHat.Code.Analysis/Abstractions` and `src/SilkHat.Code.Analysis/Services` that can resolve a repository-relative path into a full path using the repository root and optionally validate that the file belongs to a loaded solution. Use `Path.GetFullPath` on the combined path and ensure it remains under the repository root (prevent path traversal). The service returns either the file contents (as text) or an error payload indicating missing file or invalid path.

Next, add a controller in `src/SilkHat.Api/Controllers` for file content. It should use route `GET /api/repositories/{id}/code/solutions/{solutionId}/files` with a `path` query parameter (URI-encoded). Validate the query via FluentValidation (path cannot be empty) and return `ProblemDetails` on invalid input, missing repository, missing solution, or missing file. The response body should include the file contents and the repository-relative path so the UI can label tabs.

Then update the UI in `src/SilkHat.Ui/Pages/Ide.razor`. Remove the welcome tab panel and render tabs based on a list of open files. When a tree file node is clicked, call the new endpoint, open a new tab if not already present, and select the tab. Each tab should show the file contents in a scrollable `MudPaper` or `pre` block. The center panel should have a height consistent with the tree panel, and the file contents should wrap or scroll vertically without truncation. Keep the file history panel on the right intact.

If a simple syntax highlighting component is available in MudBlazor, use it for the file content area. Otherwise render plain text with a monospaced font and capture the limitation in the Decision Log.

Finally, update tests. Add controller tests in `tests/SilkHat.Api.Tests` to verify the file content endpoint calls the service and returns the right payload or `ProblemDetails`. Add service unit tests in `tests/SilkHat.Tests` for path normalization and traversal protection. Add UI tests in `tests/SilkHat.Ui.Tests` that click a file node and assert a new tab is created with the file contents.

## Concrete Steps

From `/home/vbfg/RiderProjects/SilkHat.MCP`:

    dotnet test
    docker compose up --build -d

## Validation and Acceptance

- Selecting a file node in the IDE tree opens a new tab with file contents.
- Selecting the same file again focuses the existing tab rather than duplicating it.
- The welcome tab is removed, and the center panel renders only file tabs.
- The API returns file contents from the filesystem for a valid repository-relative path, and rejects invalid or missing paths with `ProblemDetails` and an appropriate category.

## Idempotence and Recovery

The endpoint is read-only; repeated calls are safe. If path validation fails, verify normalization and repository root checks. If file contents fail to load, verify repository root and solution IDs match loaded repositories.

## Artifacts and Notes

Capture a sample response from the file content endpoint showing the file path and contents for one file in a repository, and include a short UI screenshot or description of the tab with file contents.

## Interfaces and Dependencies

- New service: `ICodeFileService` in `src/SilkHat.Code.Analysis/Abstractions` with `Task<CodeFileContentResult> GetFileAsync(string repositoryRoot, CodeSolutionWorkspace solution, string repositoryRelativePath, CancellationToken cancellationToken)`.
- New model DTO in `src/SilkHat.Code.Core/Dtos`: `CodeFileContentDto` with `Path` and `Content` fields.
- New API controller: `CodeFilesController` in `src/SilkHat.Api/Controllers` with `GET /api/repositories/{id}/code/solutions/{solutionId}/files?path=...`.
- New validation model in `src/SilkHat.Api/Models` and validator in `src/SilkHat.Api/Validation` for `path` query.

## Test Plan

- Controller tests: ensure repository and solution lookup, service calls, and response payloads are correct; verify invalid path returns 400 with validation category.
- Service unit tests: verify path normalization, path traversal rejection, missing file handling, and successful file content read.
- UI tests: render IDE, click a file item, assert a new tab appears with the expected file text, and assert selecting the same file reuses the existing tab.

Plan update (2026-01-06 23:17Z): Added controller/service/UI implementation scope and decisions for file endpoints and syntax highlighting availability.
