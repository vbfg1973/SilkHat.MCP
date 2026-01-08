# API Notes

Base path: `/api`

Key endpoints (selected):
- Health: `GET /api/health`
- Repository configs: `GET /api/repositories`, `GET /api/repositories/loaded`, `POST /api/repositories`
- Repository groups: `GET /api/repository-groups`, `POST /api/repository-groups/{id}/load`
- Code solutions: `GET /api/repositories/{id}/code/solutions`
- Code tree (lazy): `GET /api/repositories/{id}/code/solutions/{solutionId}/tree?parentId=...`
- Code files: `GET /api/repositories/{id}/code/solutions/{solutionId}/files?path=...`
- Git file last-change: `GET /api/repositories/{id}/git/files/{path}/last-change?includeDiff=...`
- Git commits: `GET /api/repositories/{id}/git/commits?...filters...`

List endpoints accept `pageNumber` and `pageSize` query parameters and return a paged payload shaped as `{ items, pageNumber, pageSize, totalCount }`.

For the full list, see `prompts/01-InitialDevelopment/08_API_SURFACE.md`.
