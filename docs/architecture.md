# Architecture

SilkHat analyzes .NET repositories by loading repository groups, parsing solutions and projects, and exposing analysis results over a REST API for the UI.

## Major Components

- API (`src/SilkHat.Api`): ASP.NET Core controllers, ProblemDetails responses, and CORS for the UI.
- Analysis runtime (`src/SilkHat.Analysis`): repository load orchestration, command processing, and runtime state.
- Code analysis (`src/SilkHat.Code.Analysis`): solution parsing, workspace construction, code tree entries, and code file reads.
- Git analysis (`src/SilkHat.Git.Analysis`): git CLI integration with caches for history and tree metadata.
- Infrastructure (`src/SilkHat.Infrastructure`): EF Core persistence with PostgreSQL.
- UI (`src/SilkHat.Ui`): Blazor WASM IDE view, repository group loader, and solution-scoped navigation.

## Runtime Flow

1. Repository groups are configured and loaded via API endpoints.
2. The analysis layer builds a `CodeRepositoryWorkspace` per loaded repository and stores it in memory.
3. Code endpoints are solution-scoped and query the workspace for projects, namespaces, types, and tree entries.
4. File contents are read directly from disk using repository roots and tree entries for fast access.
5. The UI requests tree nodes lazily and opens file tabs when a file node is selected.

## Isolation Rules

- Each loaded repository is isolated; a `LoadedRepository` does not reference other repositories.
- Paths returned by APIs are normalized to repo-relative paths using `./` prefixes and forward slashes.

## Command Streaming

Repository load operations use channels and NDJSON streaming. The API exposes a load endpoint that emits progress and data events to the UI while work proceeds.
