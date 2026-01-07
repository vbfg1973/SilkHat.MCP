M6: Hardening + Docs + Scripts

Summary:
- add helper scripts for build/test/run and docker workflows
- expand documentation with architecture overview, operations, API notes, and troubleshooting
- document ports, postgres/pgAdmin defaults, and REPO_ROOT usage
- record performance review results (existing workspace/tree caches sufficient)

Key files:
- scripts/build.sh
- scripts/test.sh
- scripts/test-integration.sh
- scripts/run-api.sh
- scripts/run-ui.sh
- scripts/docker-up.sh
- scripts/docker-down.sh
- docs/README.md
- docs/architecture.md
- docs/operations.md
- docs/api.md
- docs/troubleshooting.md
- prompts/01-InitialDevelopment/state/EXECPLAN_M6.md

Notes:
- performance pass concluded no additional caches were required; workspace/tree caches already cover LoadedRepository indices

Tests:
- dotnet test (fails in this environment: MSBuild named pipe SocketException permission denied)

Docker:
- REPO_ROOT=/path/to/repos docker compose up --build -d (fails in this environment: Docker daemon socket permission denied)
