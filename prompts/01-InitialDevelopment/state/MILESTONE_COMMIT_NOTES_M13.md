M13: add EF base entity configs and DocID symbol descriptions

- introduce BaseEntity with timestamps, move EF mappings into IEntityTypeConfiguration classes, and update DbContext timestamp handling
- add RepositorySolutionConfig timestamps with migration and switch infrastructure to Npgsql provider
- add DocID-based symbol description DTOs/services and new API endpoints for types, methods, properties, fields, events, and namespaces
- refactor call stack mermaid generation into a dedicated service and update controller/tests accordingly
- expand sample solution with an event and add unit/controller tests for DocID resolution and symbol descriptions

Tests:
- dotnet clean
- dotnet test
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose up --build -d
- REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose down
