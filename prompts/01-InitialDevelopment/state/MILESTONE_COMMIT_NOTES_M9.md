M9: add IDE symbol outline popup with file-scoped types/members

Why:
- Provide a first-class type/member outline for the active file, laying groundwork for future symbol-based navigation.

What:
- Added API endpoint `/api/repositories/{id}/code/solutions/{solutionId}/files/symbols?path=...` and Roslyn-based service to return named types plus member symbols.
- Introduced symbol outline DTOs and UI models; wired RepositoryApiClient and new Fluxor state/actions/effects for loading and tracking the popup.
- Added MudDialog popup with MudTreeView + MudCard nodes, opened from a neutral “Symbols” button left of “Close all tabs,” and kept in sync with the active tab.
- Updated UI architecture docs with new state/effects and endpoint usage.
- Added service/controller/UI tests for symbol outline and popup rendering.

Tests:
- `dotnet test -m:1`

Runtime verification:
- `REPO_ROOT=/home/vbfg/repos/c/dev/repos docker-compose up --build -d`
- IDE view: open a file tab, click “Symbols,” verify the popup shows named types and members and updates when switching tabs.
