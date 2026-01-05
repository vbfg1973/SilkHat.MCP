# System Rules (Non-Negotiable)

We are building a .NET 10 solution in C# for analyzing dotnet development repositories.

## Architectural constraints
- A "LoadedRepository" MUST NOT reference or know about any other repository.
- All interaction with repositories is done via:
  - Commands sent over channels
  - Unbounded streamed responses (progress + data + completion/failure)

## Path rules
- Repository config stores fully qualified repo root path.
- All file paths in DTOs are normalized repo-relative: "./path/from/root"
- Normalize separators to "/".

## Git access
- Do NOT use LibGit2Sharp.
- Use git CLI via Process execution and parse output.
- Fail gracefully if git is not available.

## UI
- Blazor WASM + MudBlazor, standard theme support, light/dark toggle.
- No bespoke styling system.

## Quality
- Simple, readable code. Explicit DTOs. No Roslyn symbols across boundaries.
- Always keep the system working at the end of each milestone.
