# Docker Requirements (Every Milestone)

## Containers
1) API container
- ASP.NET Core
- Expose 8080 (internal), publish to host 5000 (example)
- Include health endpoint and swagger

2) UI container
- Build WASM in build stage
- Serve with nginx in runtime stage
- Expose 80, publish to host 5001 (example)

## docker-compose.yml (root)
- services:
  - api
  - ui
- api must be reachable by ui:
  - UI config points to api service name in docker network
  - Provide runtime configuration via appsettings or environment variable

## Persistence
- SQLite DB file stored in mounted volume for api container.

Acceptance:
- `docker compose up --build` brings up both services.
- UI loads and can reach API /api/health.
