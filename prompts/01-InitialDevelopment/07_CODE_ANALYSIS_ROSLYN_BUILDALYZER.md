# Code Analysis

Use Buildalyzer + Roslyn to load/compile in-memory.

Goals:
- Load one or more .sln per LoadedRepository (defaults from config)
- Build project graph and compilation objects
- Index:
  - projects + references + referenced-by
  - namespaces (prefix filter)
  - named types with filters:
    - path prefix, namespace prefix, partial name, type kind enum, defined vs external
  - symbol lookup by SymbolKey + expected kind

Packages:
- Attempt to read resolved graph from project.assets.json to differentiate direct vs transitive.
- If unavailable, fall back to direct PackageReference only.

Never return Roslyn symbols over API; map to DTOs.
