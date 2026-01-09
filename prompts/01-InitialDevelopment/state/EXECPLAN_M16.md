# M16: GitHub Actions pipeline for versioned image publishing

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root and `.agents/PLANS.md`. It must remain fully self-contained.

## Purpose / Big Picture

After this change, the repository will have a GitHub Actions pipeline that runs all tests (unit + integration) on `master`, creates a version tag (`1.0.<minor>` starting at `1.0.1`), and publishes the API/UI images to GitHub Container Registry under that version tag only when tests pass. The outcome is observable by pushing to `master`, seeing a tag created, and seeing `ghcr.io/.../silkhat-api:<version>` and `ghcr.io/.../silkhat-ui:<version>` published.

## Progress

- [x] (2026-01-09 23:49Z) Added GitHub Actions workflow for tests on all branches, integration tests on `develop`, and publishing on `master`.
- [x] (2026-01-09 23:49Z) Implemented tag calculation for `1.0.<minor>` with initial `1.0.1` and tag existence guard.
- [x] (2026-01-09 23:49Z) Updated `docs/operations.md` with release pipeline and tag/image naming.
- [ ] (2026-01-09 23:49Z) Validation pending: run tests locally; CI publish requires `master` push.

## Surprises & Discoveries

- None yet.

## Decision Log

- Decision: Use a GitHub Actions workflow that triggers on `push` to `master` and publishes only from that branch.
  Rationale: This matches the requirement that publishing only occurs from `master` after passing all tests.
  Date/Author: 2026-01-09 / Codex
- Decision: Use `ghcr.io/<owner>/silkhat-api` and `ghcr.io/<owner>/silkhat-ui` for image names and tag them with the computed version.
  Rationale: Aligns with existing API/UI Dockerfiles and keeps registry naming consistent.
  Date/Author: 2026-01-09 / Codex
- Decision: Compute the next minor version by scanning existing tags matching `1.0.*` and incrementing the highest minor; if none exist, use `1.0.1`.
  Rationale: Avoids manual edits while honoring the requirement to tag before publishing.
  Date/Author: 2026-01-09 / Codex

## Outcomes & Retrospective

- Pending.

## Context and Orientation

The repository currently has no `.github/workflows` directory. Docker images are built via `src/SilkHat.Api/Dockerfile` and `src/SilkHat.Ui/Dockerfile`, and `docker-compose.yml` references these builds. Tests are invoked by `scripts/test.sh` and integration tests by `scripts/test-integration.sh` (uses Testcontainers). The requirement is to publish images to GitHub Container Registry only on `master`, and only after all tests (including integration tests) pass. A version tag must be created before publishing; major is fixed at `1.0` and the minor starts at `1`.

## Plan of Work

First, add a GitHub Actions workflow that runs on pushes to `master`. The workflow will check out the repo with tags, run `scripts/test.sh` and `scripts/test-integration.sh`, compute the next `1.0.<minor>` tag, create and push that tag, then build and push the `silkhat-api` and `silkhat-ui` images to GHCR with the computed version tag. The workflow must set permissions for `contents: write` (tag push) and `packages: write` (GHCR publish). The workflow should avoid running on tags to prevent recursion.

Second, add a small shell script or inline step to compute the next version. The logic should:
- Fetch all tags.
- Filter tags matching `^1\.0\.[0-9]+$`.
- If none exist, use `1.0.1`.
- Otherwise, take the highest minor number, increment it, and form the next tag.

Third, update documentation (likely `docs/operations.md`) with a short section describing the release pipeline, the tagging rule, and the image names.

Finally, validate by running tests locally and noting the docker-compose limitation. Since publishing happens only in GitHub Actions, local validation focuses on `dotnet test` and `scripts/test-integration.sh`.

## Concrete Steps

1) Add workflow file
- Create `.github/workflows/publish-images.yml`.
- Add a `push` trigger for `master` only.
- Use actions:
  - `actions/checkout@v4` with `fetch-depth: 0` to access tags.
  - `actions/setup-dotnet@v4` for .NET 9.
  - `docker/login-action@v3` for GHCR with `GITHUB_TOKEN`.
  - `docker/build-push-action@v5` for API/UI images.
- Add steps:
  - `scripts/test.sh`
  - `scripts/test-integration.sh`
  - Version compute step outputs `VERSION_TAG`.
  - Tag creation and push using git.
  - Build + push API and UI images with tag `VERSION_TAG`.

2) Update docs
- Add a section in `docs/operations.md` describing release pipeline and tags.

3) Validation
- Run `dotnet test` locally.
- Run `scripts/test-integration.sh` locally if available.
- No local publish; rely on CI for GHCR push.

## Validation and Acceptance

- On push to `master`, the workflow runs tests (unit + integration). If they pass, it creates a tag `1.0.<minor>` (starting with `1.0.1`) and pushes it.
- The workflow publishes `ghcr.io/<owner>/silkhat-api:<version>` and `ghcr.io/<owner>/silkhat-ui:<version>`.
- The workflow does not publish on non-`master` branches or tag events.
- Documentation explains the tagging rule and image names.

## Idempotence and Recovery

If the workflow fails after creating a tag but before publishing images, rerun the workflow (or re-push `master`) after deleting the tag or incrementing the minor. The workflow should fail fast if the computed tag already exists to avoid accidental overwrites.

## Artifacts and Notes

Expected tags:

    1.0.1
    1.0.2

Expected image names:

    ghcr.io/<owner>/silkhat-api:1.0.1
    ghcr.io/<owner>/silkhat-ui:1.0.1

## Interfaces and Dependencies

Workflow dependencies:
- GitHub Actions runner with Docker enabled.
- `scripts/test.sh` and `scripts/test-integration.sh` must remain runnable on the runner.
- `GITHUB_TOKEN` with `contents: write` and `packages: write` permissions.

## Test Plan

- Ensure `dotnet test` passes locally.
- Ensure `scripts/test-integration.sh` passes locally when Docker is available.
- After merge to `master`, confirm the Actions run completes, tag is created, and GHCR images are published with the version tag.

Plan Update Notes: 2026-01-09 — Initial M16 ExecPlan drafted for GitHub Actions image publishing with version tags.
Plan Update Notes: 2026-01-09 — Implemented workflow and documentation updates; validation pending CI run on `master`.
