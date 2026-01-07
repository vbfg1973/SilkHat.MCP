# API Notes

Base path: `/api`

Key endpoints (selected):
- Health: `GET /api/health`
- Repository configs: `GET /api/repositories`, `GET /api/repositories/loaded`, `POST /api/repositories`
- Repository groups: `GET /api/repository-groups`, `POST /api/repository-groups/{id}/load`
- Code solutions: `GET /api/repositories/{id}/code/solutions`
- Code tree (lazy): `GET /api/repositories/{id}/code/solutions/{solutionId}/tree?parentId=...`
- Code files: `GET /api/repositories/{id}/code/solutions/{solutionId}/files?path=...`

For the full list, see `prompts/01-InitialDevelopment/08_API_SURFACE.md`.
