M10: refine call stack resolution + decision handling + dialog-based UI tests

Why:
- Keep call stack analysis stable across SymbolKey API visibility changes, resolve interface property implementations correctly, and preserve existing test intent while wiring dialogs properly.

What:
- Added SymbolKey fallback resolution by comparing generated keys when reflection-based resolve is unavailable.
- Resolved interface property/event implementations by mapping to the correct accessor method.
- Aligned IdeSolutionEntry namespace usage for existing test imports without altering assertions.
- Updated call stack UI tests to render via MudDialogProvider/IDialogService (same assertions, correct dialog wiring).
- Added a Roslyn EmitAsync test helper and clarified the test stability rule in architecture docs.

Tests:
- `dotnet test` (blocked: MSBuild named pipe socket permission denied).

Runtime verification:
- `REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d` (blocked: docker socket permission denied).
