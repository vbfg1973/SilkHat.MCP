# SilkHat

SilkHat is a .NET-based analyzer for dotnet repositories. It includes an ASP.NET Core API, a Blazor WASM UI served by nginx, and PostgreSQL persistence.

## Quickstart

- Build: `./scripts/build.sh`
- Test: `./scripts/test.sh`
- Run API: `./scripts/run-api.sh`
- Run UI: `./scripts/run-ui.sh`
- Docker: `REPO_ROOT=/path/to/repos ./scripts/docker-up.sh`

## Ports (Docker)

- API: `http://localhost:18080/api/health`
- UI: `http://localhost:10080`
- pgAdmin: `http://localhost:10081`
- Postgres: `localhost:15432`

## Documentation

- `docs/architecture.md` for module overview and runtime flow
- `docs/api.md` for API notes and endpoint highlights
- `docs/operations.md` for local and Docker run steps
- `docs/troubleshooting.md` for common issues and fixes
