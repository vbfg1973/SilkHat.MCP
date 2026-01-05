# API Surface (MVP + growth)

Base: /api

Health:
- GET /api/health

Repository configs:
- GET  /api/repositories
- GET  /api/repositories/{id}
- POST /api/repositories
- PUT  /api/repositories/{id}

Groups:
- GET  /api/repository-groups
- GET  /api/repository-groups/{id}
- POST /api/repository-groups
- PUT  /api/repository-groups/{id}

Repo load (streaming):
- POST /api/repositories/{id}/load   (NDJSON stream RepoEvent)
- POST /api/repositories/{id}/unload

Code:
- GET /api/repositories/{id}/code/projects?name=...
- GET /api/repositories/{id}/code/projects/{projectKey}
- GET /api/repositories/{id}/code/projects/{projectKey}/references
- GET /api/repositories/{id}/code/projects/{projectKey}/referenced-by
- GET /api/repositories/{id}/code/namespaces?prefix=...
- GET /api/repositories/{id}/code/named-types?...filters...
- POST /api/repositories/{id}/code/symbols/lookup

Git:
- GET /api/repositories/{id}/git/tree?...filters...
- GET /api/repositories/{id}/git/files/history/{*path}
- GET /api/repositories/{id}/git/files/cochanges/{*path}

Requirements:
- Swagger enabled
- ProblemDetails with correlationId + failure category
