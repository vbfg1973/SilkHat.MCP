# Add Method Call Stack Analysis + Decision Resolution + Mermaid View

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repository root as `PLANS.md`; this document must be maintained in accordance with it.

## Purpose / Big Picture

After this change, a user can select a method in the IDE symbol popup and open a new call stack popup that traces every method call made by that method, recursively, across the solution. The call stack view includes a hierarchical tree view and a Mermaid sequence diagram view, plus the ability to open or jump to the call site in the IDE file tabs. When a call targets an interface, the system resolves it to a concrete implementation using persisted decisions, or asks for a decision when multiple non-test implementations exist. The system records any decision used in responses, so clients can surface the context and allow future refinements.

## Progress

- [x] (2026-01-08 20:10Z) Create this ExecPlan and confirm scope/constraints.
- [x] (2026-01-09 10:40Z) Implement method call stack analysis service with interface resolution decisions.
- [x] (2026-01-09 10:40Z) Add decision persistence (EF Core + migrations) and decision-aware response metadata.
- [x] (2026-01-09 10:40Z) Add API endpoints for call stack and Mermaid sequence output.
- [x] (2026-01-09 10:40Z) Add UI popup with tree + Mermaid views and call-site navigation.
- [x] (2026-01-08 19:24Z) Re-checked milestone requirements and verified existing implementation coverage to avoid touching existing tests.
- [x] (2026-01-08 19:26Z) Updated architecture notes to explicitly record decision-using service methods and response fields.
- [x] (2026-01-08 19:27Z) Corrected ExecPlan details to match the current decision service location and decision entity indexing.
- [x] (2026-01-08 19:33Z) Reworked SymbolKey resolution to use reflection-based helper and aligned `IdeSolutionEntry` namespace with existing test expectations.
- [x] (2026-01-08 19:33Z) Added a test helper extension for `Compilation.EmitAsync` to keep existing tests unchanged.
- [x] (2026-01-08 19:35Z) Documented test stability rule in architecture docs (no changes to existing test intent or assertions; wiring/mocks allowed).
- [x] (2026-01-08 20:10Z) Added SymbolKey fallback resolution when reflection fails and improved interface property implementation resolution.
- [x] (2026-01-08 20:10Z) Updated call stack UI tests to render dialogs via MudDialogProvider/IDialogService without changing assertions.
- [x] (2026-01-08 20:17Z) Wired MudPopoverProvider and call stack state into dialog-based UI tests.
- [x] (2026-01-08 20:17Z) Added interface-implementation fallback matching by name/signature when Roslyn symbol equality fails.
- [x] (2026-01-08 20:21Z) Added signature-based candidate fallback by namespace and relaxed stored-decision resolution.
- [x] (2026-01-08 20:40Z) Added cross-namespace signature fallback when interface-scoped lookup returns no candidates.
- [x] (2026-01-09 11:05Z) Added Roslyn documentation ID support to persist stable interface/implementation identities.
- [x] (2026-01-09 11:05Z) Added documentation ID resolution tests that reload the sample solution and re-resolve symbols by doc ID.
- [ ] (2026-01-08 20:10Z) Run `dotnet test` (blocked: MSBuild named pipe socket permission denied).
- [ ] (2026-01-08 19:34Z) Run `docker compose up --build` (blocked: Docker daemon socket permission denied).
- [ ] (2026-01-09 10:40Z) Add unit, integration, controller, and UI tests per testing standard (completed: new tests added; remaining: run/verify).
- [ ] Validate with tests and docker compose; produce milestone commit notes.

## Surprises & Discoveries

- Observation: `dotnet test` aborts with `System.Net.Sockets.SocketException (13): Permission denied` during VSTest communication startup.
  Evidence: `SocketServer.Start` failures while running `dotnet test -m:1` from the repository root.
- Observation: `docker compose up --build` cannot connect to `/var/run/docker.sock`.
  Evidence: `connect: operation not permitted` when pulling `postgres:latest`.
- Observation: `dotnet test` also fails earlier with MSBuild named pipe startup (`MSB1025`) before tests execute.
  Evidence: `NamedPipeServerStream` permission errors when running `dotnet test` without `-m:1`.
- Observation: Dialog-based MudBlazor components require `MudPopoverProvider` even in tests, otherwise dialog rendering fails.
  Evidence: `Missing <MudPopoverProvider />` exceptions in `IdeCallStackPopupTests` runs.
- Observation: Interface implementation resolution can yield no candidates when symbol identity differs across compilations.
  Evidence: `ResolveAsync_ReturnsSingleImplementation_WhenOnlyOneExists` returning null implementation.
- Observation: Roslyn documentation IDs remain stable across workspace reloads for sample solution symbols.
  Evidence: `DocumentationIdResolutionTests` resolve method/type doc IDs after reloading `samples/solution01`.

## Decision Log

- Decision: Open the call stack popup directly from `IdeSymbolPopup` after dispatching `OpenCallStackPopupAction`.
  Rationale: Keep call stack dialog lifecycle with the initiating component and avoid forcing additional state subscriptions into `IdeTabs` that would require updates to existing tests.
  Date/Author: 2026-01-09 / Codex
- Decision: Move `IdeSolutionEntry` into the `SilkHat.Ui.State.Ide` namespace to satisfy existing test imports without editing test files.
  Rationale: Preserve the “no edits to existing tests” constraint while keeping production changes small and localized.
  Date/Author: 2026-01-08 / Codex
- Decision: Add a test helper extension for `Compilation.EmitAsync` in the test project rather than editing the test file.
  Rationale: Keep existing tests unchanged while resolving an API surface mismatch.
  Date/Author: 2026-01-08 / Codex
- Decision: Render call stack UI tests through MudDialogProvider/IDialogService to match runtime rendering while keeping assertions intact.
  Rationale: Preserve test intent and assertions while adjusting wiring for dialog-based components.
  Date/Author: 2026-01-08 / Codex
- Decision: Add SymbolKey fallback resolution by comparing generated keys across compilations when reflection-based resolution fails.
  Rationale: Maintain call stack functionality even when SymbolKey APIs are inaccessible.
  Date/Author: 2026-01-08 / Codex
- Decision: Fall back to name/signature-based interface implementation matching when Roslyn symbol equality does not produce candidates.
  Rationale: Ensure interface resolution still works when interface symbols come from different compilations or metadata contexts.
  Date/Author: 2026-01-08 / Codex
- Decision: Add namespace-scoped signature fallback when no interface candidates are found, and trust stored decision type names.
  Rationale: Restore interface resolution for sample solution/test fixtures while keeping false positives constrained.
  Date/Author: 2026-01-08 / Codex
- Decision: Add a final signature-based candidate fallback without namespace filtering when prior resolution yields no candidates.
  Rationale: Ensure interface resolution still produces candidates in minimal or synthetic compilation scenarios.
  Date/Author: 2026-01-08 / Codex
- Decision: Persist Roslyn documentation IDs (DocID) for interface/implementation symbols and prefer them during decision resolution.
  Rationale: DocIDs remain stable across workspace reloads and allow decisions to survive compilation boundaries.
  Date/Author: 2026-01-09 / Codex

## Outcomes & Retrospective

Pending implementation.

## Context and Orientation

The IDE uses Fluxor state slices in `src/SilkHat.Ui/State/Ide` and popup components under `src/SilkHat.Ui/Components/Ide`. The symbol outline popup is `src/SilkHat.Ui/Components/Ide/IdeSymbolPopup.razor` and is triggered from `src/SilkHat.Ui/Components/Ide/IdeTabs.razor`. Code analysis data lives in `src/SilkHat.Code.Analysis`, with solution workspaces in memory (`CodeSolutionWorkspace`) and Roslyn compilations used for symbol and syntax inspection. API controllers for code-related features are in `src/SilkHat.Api/Controllers`.

We now need a new “method call stack” analysis. The call stack is a recursive graph of method invocations originating from a specific method symbol. Each call site produces a node with a stable ID that includes the depth, a fully qualified method identifier (namespace, type, method name, parameter type list), and the call index within the parent method’s call list. Each node also includes the call site location (file path + span) and a line span to enable syntax highlighting later.

We also need a decision system that resolves interface calls to concrete implementations. If a call is to an interface method, we choose a concrete implementation using rules: if there is one implementation, use it; if multiple implementations exist but only one is outside test projects, use the non-test one; if multiple non-test implementations exist, we require a user decision. Decisions are stored in the database and are solution-scoped. Whenever a decision is used by a service, the response must include the decision id and type at the relevant node; when no decision applies, the field remains present but null. We must also maintain a record of which services use decisions and which response types carry decision metadata, and record this as an architectural rule in `docs/architecture.md`.

Decisions should also persist Roslyn documentation IDs for interface and implementation symbols. These documentation IDs (DocID) are stable across workspace reloads and allow decision resolution to survive compilation boundaries. Response models should expose documentation IDs for candidate methods so the UI can persist/round-trip stable identifiers.

This milestone may add new tests, but existing test files are not to be modified. If a change would otherwise require updating an existing test, the implementation must be adjusted to keep current tests passing.

## Plan of Work

Implement the method call stack as a new analysis service in `src/SilkHat.Code.Analysis` and expose it via a new method-specific API controller. The service should accept a solution workspace and a starting method symbol (SymbolKey), then build a flat list of call nodes with parent-child relationships (via parent node id and depth). To produce call sites, traverse the method body syntax tree, find invocation expressions and object creations, resolve the symbol for each call, and map the call target to a method symbol. The service should recursively analyze each method call until no further calls are found. Guard against recursion cycles by tracking method symbol keys per traversal path.

Add a new decision entity and persistence via EF Core migrations. Provide a decision service (implemented in the API layer where `SilkHatDbContext` is already registered) that can resolve an interface method to a concrete implementation based on existing decisions, and also return candidate implementations when a decision is needed. Add API endpoints to list and persist decisions for a solution. If the call stack evaluation uses a decision, include the decision metadata in the corresponding call node. If a decision is missing and multiple non-test implementations exist, include a “decision required” marker in the response so the UI can prompt the user.

Create a new call stack controller in `src/SilkHat.Api/Controllers`, with endpoints to return the call stack node list and a Mermaid sequence diagram derived from the same call stack result. Both endpoints are solution-scoped (`/api/repositories/{id}/code/solutions/{solutionId}/methods/...`). The Mermaid endpoint should reference interface names in the diagram when the original call target was an interface, and add the resolved concrete type in italics parentheses.

Add a new popup in the UI (MudDialog) with tabs: a hierarchical view and a Mermaid view. The tree view should allow expand/collapse, show an icon for each node, and allow jumping to the call site in the IDE tabs. The popup should be resizable/full-screen capable (using MudDialog options). Add a new button in the symbol popup to open the call stack popup for the selected method. Ensure the call stack popup updates when a different method is selected and tracks any decision metadata returned by the API.

When a call stack node reports `DecisionRequired` with candidate implementations, the popup should expose a select-and-save workflow that persists the decision through the decision endpoint and then reloads the call stack and Mermaid views.

Update `docs/architecture.md` with the decision rule, the new analysis services, and the new API endpoints. Add tests: service unit tests for call stack analysis, decision resolution, controller tests for call stack + Mermaid endpoints, and UI tests for popup rendering and navigation interactions.

## Concrete Steps

Work from `/home/vbfg/RiderProjects/SilkHat.MCP`.

1) Data model and persistence for decisions.
   - Add a new entity in `src/SilkHat.Infrastructure/Entities` (for example, `MethodImplementationDecision`) with:
     - Id (Guid)
     - RepositoryConfigId (Guid)
     - SolutionId (string)
     - InterfaceTypeName (string, fully qualified)
     - InterfaceMethodSignature (string, fully qualified with parameter types)
     - ImplementationTypeName (string, fully qualified)
     - InterfaceTypeDocumentationId (string, optional)
     - InterfaceMethodDocumentationId (string, optional)
     - ImplementationTypeDocumentationId (string, optional)
     - ImplementationMethodDocumentationId (string, optional)
     - CreatedUtc / UpdatedUtc (DateTimeOffset)
   - Add `DbSet<MethodImplementationDecision>` to `SilkHatDbContext` and configure in `OnModelCreating` (unique index on RepositoryConfigId + SolutionId + InterfaceMethodSignature).
   - Add migration and update model snapshot.

2) Decision service.
   - Add `IMethodImplementationDecisionService` in `src/SilkHat.Code.Analysis/Abstractions` with methods to:
     - Resolve concrete implementation for an interface method.
     - Return candidate implementations + “decision required” status.
     - Persist a decision.
   - Implement in `src/SilkHat.Api/Services` using `SilkHatDbContext`, preferring documentation IDs for matching when available and falling back to signature/type-name matching.

3) Call stack analysis service.
   - Add `IMethodCallStackService` in `src/SilkHat.Code.Analysis/Abstractions` with a method `BuildCallStackAsync(...)`.
   - Implement in `src/SilkHat.Code.Analysis/Services`:
     - Input: `CodeSolutionWorkspace`, starting method `SymbolKey` (string), cancellation token.
     - Output: `MethodCallStackResult` containing list of `MethodCallStackNode` with:
       - NodeId, Depth, Index
       - MethodName, Namespace, TypeName, FullMethodSignature, ParameterTypes
       - ParentNodeId
       - CallSiteLocation (Path + TextSpan + line span)
       - Decision metadata (DecisionId, DecisionType) or null
       - InterfaceTarget info (if applicable)
     - Track and avoid cycles.
     - Use `SemanticModel` to resolve `InvocationExpressionSyntax` and `ObjectCreationExpressionSyntax` to method symbols.
     - Use decision service for interface calls.

4) API controller endpoints.
   - Add a new controller `MethodCallStackController` under `src/SilkHat.Api/Controllers` with:
     - `POST /api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack`
     - `POST /api/repositories/{id}/code/solutions/{solutionId}/methods/call-stack/mermaid`
   - Request body: `MethodCallStackRequestDto` containing `SymbolKey` and optional `MaxDepth`.
   - Response DTO: `MethodCallStackResponseDto` containing node list and decision metadata.
   - Mermaid response: `MethodCallStackMermaidDto` with a `Diagram` string.
   - Ensure ProblemDetails with correlation id are used for errors.

5) UI popup + Fluxor state.
   - Add a new popup component `src/SilkHat.Ui/Components/Ide/IdeCallStackPopup.razor`.
   - Add state/actions/effects in `src/SilkHat.Ui/State/Ide` for call stack:
     - Open/close popup, load call stack, load Mermaid, handle errors.
   - Add UI models for call stack nodes and response metadata.
   - Add a button in `IdeSymbolPopup` to open the call stack popup for the selected method.
   - Implement tree view with icon + click to open/jump to call site in tabs (use existing tab open/jump behavior).
   - Add Mermaid view tab that renders the sequence diagram string (initially plain text display; follow MudBlazor guidance for display).

6) Update docs.
   - Update `docs/architecture.md` with decision rule, decision service usage, call stack analysis service, new endpoints, UI state/actions/effects, and DocID persistence rules.

7) Tests.
   - Service unit tests in `tests/SilkHat.Tests` for call stack analysis (sample solution under `samples`), including interface resolution paths.
   - Service unit tests for DocID resolution across workspace reloads using `samples/solution01`.
   - Service tests for decision resolution rules (single impl, only non-test impl, multiple impls -> decision required).
   - Controller tests in `tests/SilkHat.Api.Tests` for call stack and Mermaid endpoints.
   - UI tests in `tests/SilkHat.Ui.Tests` for call stack popup rendering and for the “open call stack” button in symbol popup dispatching actions.

## Validation and Acceptance

Run:

    dotnet test -m:1

Then:

    REPO_ROOT=/home/vbfg/repos/c/dev/repos docker-compose up --build -d

Acceptance criteria:

- Selecting a method in the symbol popup can open a call stack popup.
- The call stack popup shows a tree of calls and can jump to a call site in the IDE file tabs.
- A Mermaid sequence diagram can be retrieved via API and displayed in the popup.
- Interface call resolution follows the decision rules, and decision metadata is included in responses (null when none).

## Idempotence and Recovery

Migrations should be safe to rerun; if the database already contains the decision table, the migration should no-op. If a decision is missing, the call stack response should mark the node as “decision required” and allow the UI to prompt the user without failing the overall call stack build.

## Artifacts and Notes

Capture:
- Example call stack response snippet.
- Example Mermaid diagram output.
- Test output summary.

## Interfaces and Dependencies

Define in `src/SilkHat.Code.Core/Dtos`:

    public sealed record MethodCallStackRequestDto(string SymbolKey, int? MaxDepth);

    public sealed record MethodCallStackNodeDto(
        string NodeId,
        int Depth,
        int Index,
        string CallerNamespace,
        string CallerTypeName,
        string CallerMethodName,
        IReadOnlyList<string> CallerParameterTypes,
        string CallerFullyQualifiedMethodName,
        string Namespace,
        string TypeName,
        string MethodName,
        IReadOnlyList<string> ParameterTypes,
        string FullyQualifiedMethodName,
        string? ParentNodeId,
        CodeLocationDto CallSite,
        DecisionInfoDto? Decision,
        bool IsInterfaceTarget,
        string? InterfaceTypeName,
        string? InterfaceTypeDocumentationId,
        string? InterfaceMethodDocumentationId,
        string? ResolvedTypeName,
        string? ResolvedTypeDocumentationId,
        string? ResolvedMethodDocumentationId,
        bool DecisionRequired,
        IReadOnlyList<string> CandidateTypeNames,
        IReadOnlyList<string?> CandidateMethodDocumentationIds);

    public sealed record MethodCallStackResponseDto(
        IReadOnlyList<MethodCallStackNodeDto> Nodes);

    public sealed record MethodCallStackMermaidDto(string Diagram);

    public sealed record DecisionInfoDto(Guid Id, string Type);

Add supporting `CodeLocationDto` if not already present, including file path and text span + line span.

## Test Plan

- `tests/SilkHat.Tests`: call stack service tests + decision resolution tests.
- `tests/SilkHat.Tests`: documentation ID resolution tests that reload `samples/solution01` and re-resolve method/type DocIDs.
- `tests/SilkHat.Api.Tests`: call stack controller tests.
- `tests/SilkHat.Ui.Tests`: call stack popup renders nodes and Mermaid string; symbol popup open-call-stack dispatches action.

Run `dotnet test -m:1` and verify all tests pass.

Plan updated on 2026-01-08 to capture the SymbolKey fallback resolution, dialog-based UI test wiring updates, and the latest test execution blockers.
Plan updated on 2026-01-08 to capture MudPopoverProvider wiring in tests and interface-implementation fallback matching.
Plan updated on 2026-01-08 to capture namespace-scoped signature fallback for interface implementations.
Plan updated on 2026-01-08 to capture the cross-namespace signature fallback for candidate resolution.
Plan updated on 2026-01-09 to capture DocID persistence, matching rules, and test coverage for documentation ID resolution.
