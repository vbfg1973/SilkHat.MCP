# M17: Git branch endpoints, file change counts, and controller organization

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` and `.agents/PLANS.md`. It must remain fully self-contained.

## Purpose / Big Picture

After this change, an operator can query the API to learn how many commits a file has on the current branch, see the current branch name, and list local branches. The Git controllers are grouped in folders for easier navigation. This is observable by calling the new endpoints on the running API and by passing all unit, API, and integration tests.

## Progress

- [x] (2026-01-10 00:25Z) Create Git change count capability in `IGitCli` and `GitCli`.
- [x] (2026-01-10 00:25Z) Add Git branch queries in `IGitCli`/`GitCli` and new `GitBranchesController` endpoints.
- [x] (2026-01-10 00:25Z) Add Git file change count endpoint in `GitFilesController`.
- [x] (2026-01-10 00:25Z) Move controllers into category folders without altering route behavior.
- [x] (2026-01-10 00:25Z) Add and update unit, API, and integration tests.
- [x] (2026-01-10 00:27Z) Validate with `dotnet test` and `scripts/test-integration.sh`.
- [x] (2026-01-10 02:05Z) Add HybridCache wrapper for API endpoint caching and invalidate on repository load.
- [x] (2026-01-10 02:07Z) Set cache TTL to 10 minutes and validate with `dotnet test`.

## Surprises & Discoveries

None.

## Decision Log

- Decision: Keep controller namespaces unchanged while moving files into category folders.
  Rationale: Folder organization meets the requirement without forcing a wide namespace refactor.
  Date/Author: 2026-01-10 / Codex

## Outcomes & Retrospective

Completed with passing unit, API, and integration coverage for the new Git endpoints and controller organization, plus HybridCache-backed API caching with load-time invalidation.

## Context and Orientation

The API currently exposes Git endpoints via `src/SilkHat.Api/Controllers/GitFilesController.cs`, `src/SilkHat.Api/Controllers/GitCommitsController.cs`, and `src/SilkHat.Api/Controllers/GitTreeController.cs`. Git operations are implemented in `src/SilkHat.Git.Analysis/Services/GitCli.cs` behind `src/SilkHat.Git.Analysis/Abstractions/IGitCli.cs`. Tests live in `tests/SilkHat.Tests/Services/GitCliTests.cs` (unit), `tests/SilkHat.Api.Tests/Controllers/*.cs` (API controller tests), and `tests/SilkHat.IntegrationTests/GitCommitIntegrationTests.cs` (integration Git CLI tests against the repo). The repository root for integration tests is resolved via `AppContext.BaseDirectory` and already assumes the repo under test exists.

A “file change count” means the number of commits on the current branch whose history includes the specified path, following renames. A “current branch” is the name of the checked-out local branch (as reported by `git rev-parse --abbrev-ref HEAD`). “Local branches” are the names under `refs/heads`.

## Plan of Work

First, extend `IGitCli` with methods for file change counts and branch discovery. Implement these in `GitCli` by invoking `git log --follow --pretty=format:%H -- <path>` and counting lines for the change count, and `git rev-parse --abbrev-ref HEAD` plus `git for-each-ref refs/heads --format=%(refname:short)` for branch names. Make sure the pathspec appears after pagination options and `--` to avoid the `--follow requires exactly one pathspec` error.

Second, add DTOs for the new responses in `src/SilkHat.Git.Core/Dtos` so the API responses are explicit. The file change count response should include the normalized path and count. The branch response should include the current branch name and the list of local branches (sorted alphabetically for stable output).

Third, add a new `GitBranchesController` under the Git folder with endpoints:

- `GET api/repositories/{id}/git/branches/current`
- `GET api/repositories/{id}/git/branches`

These endpoints should mirror existing Git controllers: validate repository loaded via `ILoadedRepositoryStore`, call `IGitCli`, and wrap failures with a `Git` problem category.

Fourth, add a `GET api/repositories/{id}/git/files/{path}/change-count` endpoint on `GitFilesController` that returns the change count for the given path on the current branch, with the same URI decoding behavior as the other file endpoints.

Fifth, reorganize controller files into folders:

- `src/SilkHat.Api/Controllers/Code` for code-related controllers (symbols, files, namespaces, solutions, projects, tree, methods, file symbols, symbol descriptions).
- `src/SilkHat.Api/Controllers/CodeAnalysis` for `MethodCallStackController`.
- `src/SilkHat.Api/Controllers/Decisions` for `DecisionsController` and `MethodImplementationDecisionsController`.
- `src/SilkHat.Api/Controllers/Git` for `GitFilesController`, `GitCommitsController`, `GitTreeController`, and the new `GitBranchesController`.
- `src/SilkHat.Api/Controllers/Repository` for repository discovery/load/config/group endpoints.

Keep namespaces as-is to avoid cascading changes; only file locations move.

Finally, update tests. Add Git CLI unit tests for the new commands in `tests/SilkHat.Tests/Services/GitCliTests.cs` using the fake runner. Add controller tests in `tests/SilkHat.Api.Tests/Controllers` for the new endpoints. Extend `tests/SilkHat.IntegrationTests/GitCommitIntegrationTests.cs` with integration tests for branch name and file change count against this repository.

## Concrete Steps

1) Update Git abstractions and DTOs
   - Edit `src/SilkHat.Git.Analysis/Abstractions/IGitCli.cs` to add:
     - `Task<GitFileChangeCountDto> GetFileChangeCountAsync(Guid configId, string repoRoot, string path, CancellationToken cancellationToken);`
     - `Task<string> GetCurrentBranchAsync(Guid configId, string repoRoot, CancellationToken cancellationToken);`
     - `Task<IReadOnlyList<string>> ListLocalBranchesAsync(Guid configId, string repoRoot, CancellationToken cancellationToken);`
   - Add DTOs in `src/SilkHat.Git.Core/Dtos`:
     - `GitFileChangeCountDto` with `Path` and `ChangeCount`.
     - `GitBranchListDto` with `CurrentBranch` and `LocalBranches` (list of strings).

2) Implement GitCli methods
   - Edit `src/SilkHat.Git.Analysis/Services/GitCli.cs` to implement the new methods, ensuring the `--` pathspec ordering is correct and counting output lines safely when output is empty.

3) Add API endpoints
   - Edit `src/SilkHat.Api/Controllers/GitFilesController.cs` to add `GetChangeCount` endpoint.
   - Add `src/SilkHat.Api/Controllers/Git/GitBranchesController.cs` (or move after folder organization) with `current` and `list` endpoints returning DTOs.

4) Move controllers into folders
   - Move files into the category folders listed above without changing namespaces or routes.

5) Tests
   - Update `tests/SilkHat.Tests/Services/GitCliTests.cs` with tests for branch commands and change count command building and parsing.
   - Add `tests/SilkHat.Api.Tests/Controllers/GitBranchesControllerTests.cs` and extend GitFiles controller tests for change count.
   - Extend `tests/SilkHat.IntegrationTests/GitCommitIntegrationTests.cs` with:
     - `GetCurrentBranch_ReturnsNonEmptyName`.
     - `ListLocalBranches_IncludesCurrentBranch`.
     - `GetFileChangeCount_ReturnsPositiveCount` for a known file in this repo (e.g., `docs/README.md`).

6) Validation
   - Run `dotnet test` from repo root.
   - Run `./scripts/test-integration.sh` from repo root.

## Validation and Acceptance

- `GET api/repositories/{id}/git/branches/current` returns the current branch name string.
- `GET api/repositories/{id}/git/branches` returns a list of local branches that includes the current branch.
- `GET api/repositories/{id}/git/files/{path}/change-count` returns a positive integer for `docs/README.md` on this repo.
- All unit, API, and integration tests pass via `dotnet test` and `./scripts/test-integration.sh`.

## Idempotence and Recovery

Moving controller files is safe to re-run; if a move fails, relocate the files back to `src/SilkHat.Api/Controllers` and retry the move. Git commands used for branch discovery and file counts are read-only.

## Artifacts and Notes

Expected commands (examples):

  git rev-parse --abbrev-ref HEAD
  git for-each-ref refs/heads --format=%(refname:short)
  git log --follow --pretty=format:%H -- docs/README.md

## Interfaces and Dependencies

- `SilkHat.Git.Analysis.Abstractions.IGitCli` must expose methods for branch discovery and file change counts.
- `SilkHat.Git.Analysis.Services.GitCli` must implement those methods using `IGitCommandRunner`.
- `SilkHat.Git.Core.Dtos.GitFileChangeCountDto` and `GitBranchListDto` must exist.
- `SilkHat.Api.Controllers.GitBranchesController` must expose branch endpoints.
- `SilkHat.Api.Controllers.GitFilesController` must expose the change count endpoint.

## Test Plan

Unit tests in `tests/SilkHat.Tests/Services/GitCliTests.cs` will validate the command strings and parsing for branch and change count methods using the fake command runner. API controller tests will use mocks for `IGitCli` and `ILoadedRepositoryStore` to validate successful responses and error handling. Integration tests in `tests/SilkHat.IntegrationTests` will hit the real Git CLI against the repo and assert non-empty branch names and positive change counts for a known file. Run `dotnet test` and `./scripts/test-integration.sh` from the repo root and expect all tests to pass.

Plan Update Notes: 2026-01-10 — Initial M17 ExecPlan drafted for Git branch endpoints, change counts, and controller organization.
Plan Update Notes: 2026-01-10 — Implemented Git change count + branch endpoints, reorganized controllers, and validated tests.
Plan Update Notes: 2026-01-10 — Added HybridCache wrapper for API caching, load-time invalidation, and 10-minute TTL.
