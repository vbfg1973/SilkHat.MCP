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
