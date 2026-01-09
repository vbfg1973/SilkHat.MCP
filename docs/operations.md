# Operations

This document describes how to run SilkHat locally and via Docker.

## Local

- API: `./scripts/run-api.sh`
- UI: `./scripts/run-ui.sh`
- Tests: `./scripts/test.sh`
- Integration tests: `./scripts/test-integration.sh`

Environment variables:
- `REPO_ROOT` is required when using repository discovery features in the API. Example: `export REPO_ROOT=/path/to/repos`

## Docker

Start the stack:

- `REPO_ROOT=/path/to/repos ./scripts/docker-up.sh`

Stop the stack:

- `./scripts/docker-down.sh`

Ports:
- API: `http://localhost:18080`
- UI: `http://localhost:10080`
- pgAdmin: `http://localhost:10081`
- Postgres: `localhost:15432`

Database:
- PostgreSQL user/password/database are `silkhat` by default (see `docker-compose.yml`).
- pgAdmin defaults to `admin@example.com` / `admin`.
- The API container configures git to treat all repositories under `/repos` as safe directories on startup.

## Quick Validation

- API health: `http://localhost:18080/api/health` returns `OK`.
- UI loads at `http://localhost:10080` and can read `/api/health`.

## Release Pipeline

Publishing is handled by GitHub Actions on pushes to `master`. The workflow runs unit tests and integration tests, computes the next version tag in the `1.0.<minor>` series (starting at `1.0.1`), creates and pushes that tag, then publishes container images to GHCR.

Images:
- `ghcr.io/<owner>/silkhat-api:<version>`
- `ghcr.io/<owner>/silkhat-ui:<version>`

Tags:
- Tags follow `1.0.<minor>` and are created only after all tests pass on `master`.
