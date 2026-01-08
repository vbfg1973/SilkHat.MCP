# API Surface (MVP + growth)

Base: /api

Health:
- GET /api/health

Repository configs:
- GET  /api/repositories
- GET  /api/repositories/loaded
- GET  /api/repositories/{id}
- POST /api/repositories
- PUT  /api/repositories/{id}

Groups:
- GET  /api/repository-groups
- GET  /api/repository-groups/{id}
- POST /api/repository-groups
- PUT  /api/repository-groups/{id}
- POST /api/repository-groups/{id}/load

Repo load (streaming):
- POST /api/repositories/{id}/load   (NDJSON stream RepoEvent)
- POST /api/repositories/{id}/unload

Code:
- GET /api/repositories/{id}/code/solutions
- GET /api/repositories/{id}/code/solutions/{solutionId}/projects?name=...
- GET /api/repositories/{id}/code/solutions/{solutionId}/projects/{projectKey}
- GET /api/repositories/{id}/code/solutions/{solutionId}/projects/{projectKey}/references
- GET /api/repositories/{id}/code/solutions/{solutionId}/projects/{projectKey}/referenced-by
- GET /api/repositories/{id}/code/solutions/{solutionId}/namespaces?prefix=...
- GET /api/repositories/{id}/code/solutions/{solutionId}/named-types?...filters...
- GET /api/repositories/{id}/code/solutions/{solutionId}/tree?parentId=...
- GET /api/repositories/{id}/code/solutions/{solutionId}/files?path=...
- POST /api/repositories/{id}/code/solutions/{solutionId}/symbols/lookup

Git:
- GET /api/repositories/{id}/git/tree?...filters...
- GET /api/repositories/{id}/git/commits?...filters...
- GET /api/repositories/{id}/git/files/{path}/last-change?includeDiff=...
- GET /api/repositories/{id}/git/files/{path}/history
- GET /api/repositories/{id}/git/files/{path}/cochanges

Discovery:
- GET /api/repositories/available
- GET /api/repositories/available/solutions?path=...

Requirements:
- Swagger enabled
- ProblemDetails with correlationId + failure category
- All list-returning endpoints accept `pageNumber` and `pageSize` query parameters and return a paged result payload: `{ items, pageNumber, pageSize, totalCount }`.
