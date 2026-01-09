M10: add call stack UX controls + mermaid fixes + external-call filtering

Why:
- Make call stack exploration clearer in the UI, keep Mermaid output stable with nested returns, and allow optional inclusion of external calls while defaulting to in-codebase analysis.

What:
- Added IncludeExternalCalls flag through DTOs/models, UI state, actions, reducers, and effects; default false and reload on toggle.
- Filtered call stack traversal to skip external calls unless explicitly enabled.
- Mermaid sequence rendering now preserves nesting (call -> children -> return) and fixes return line syntax.
- Stabilized Mermaid participant labeling using short type names and interface+implementation mapping to keep lanes connected.
- Added the include-external checkbox to the call stack popup.

Tests:
- `dotnet test`

Runtime verification:
- `REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose up --build -d` failed with Docker snapshot error: "parent snapshot ... does not exist".
