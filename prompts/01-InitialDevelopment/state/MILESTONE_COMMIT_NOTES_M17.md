M17: add Git file change counts, branch endpoints, and controller organization

- add Git file change count API + DTOs, backed by GitCli log parsing
- add Git branches API (current branch + local branches) with dedicated controller
- move API controllers into Code/CodeAnalysis/Decisions/Git/Repository folders without route changes
- expand unit tests for GitCli branch/change count commands and API/controller coverage
- extend integration tests to verify branch discovery and file change counts against the repo
- add HybridCache wrapper for API caching and invalidate cache on repository load
- set cache TTL to 10 minutes and document API caching in architecture notes

Tests:
- dotnet test
