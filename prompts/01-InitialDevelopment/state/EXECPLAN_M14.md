# M14: DocID-based complexity analysis for methods and concrete types

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan is maintained according to `PLANS.md` in the repository root and `.agents/PLANS.md`. It must remain fully self-contained.

## Purpose / Big Picture

After this change, users can request cognitive, cyclomatic, or indentation complexity for a specific method or for a concrete named type (class, struct, record) using DocID. They can see the resulting integer score through new API endpoints and in tests that compute known values from the sample solution. The behavior is observable by running the API endpoints with DocIDs and by running the test suite to see the complexity results validated.

## Progress

- [x] (2026-01-09 16:59Z) Create ExecPlan for M14 and capture current context.
- [x] (2026-01-09 17:18Z) Implement complexity strategy interfaces, factory, and algorithms.
- [x] (2026-01-09 17:18Z) Implement method complexity service using DocID lookup and strategy factory.
- [x] (2026-01-09 17:18Z) Implement concrete type complexity service that sums method complexities.
- [x] (2026-01-09 17:18Z) Add DTOs, API endpoints, DI wiring, and error handling.
- [x] (2026-01-09 17:20Z) Add tests for strategies, factory, services, and API endpoints; run validation with `dotnet test`.
- [x] (2026-01-09 17:27Z) Validate docker compose build/run with REPO_ROOT set and shut down containers.
- [x] (2026-01-09 17:29Z) Align record struct classification with structs in analysis and tests.

## Surprises & Discoveries

- None yet.

## Decision Log

- Decision: Use DocID as the primary identifier for complexity requests and results.
  Rationale: DocIDs are stable across compilations, already used in other symbol lookups, and are required for persistence and cross-run identification.
  Date/Author: 2026-01-09 / Codex
- Decision: Use query parameters for DocID in new endpoints rather than path segments.
  Rationale: DocIDs contain characters that are awkward in route segments; query parameters avoid encoding pitfalls.
  Date/Author: 2026-01-09 / Codex
- Decision: Treat record structs as structs for classification.
  Rationale: Record structs are value types and should be grouped with structs rather than records in type-kind outputs.
  Date/Author: 2026-01-09 / Codex

## Outcomes & Retrospective

- **Delivered:** Complexity DTOs, strategies (cognitive/cyclomatic/indentation), strategy factory, method/type complexity services, and method/named-type complexity API endpoints with DocID-first lookup.
- **Testing:** `dotnet test` passes; docker compose build/run validated with REPO_ROOT set and containers stopped.
- **Status:** Milestone complete.

## Context and Orientation

The codebase already supports DocID lookups for symbols. The DocID utility lives in `src/SilkHat.Code.Analysis/Services/DocumentationIdUtility.cs`. Symbol descriptions are provided by `src/SilkHat.Code.Analysis/Services/SymbolDescriptionService.cs`. Method call stack analysis exists in `src/SilkHat.Code.Analysis/Services/MethodCallStackService.cs`, with API endpoints in `src/SilkHat.Api/Controllers/MethodCallStackController.cs`. Named type listing lives in `src/SilkHat.Api/Controllers/CodeNamedTypesController.cs`. There are no existing complexity services or endpoints, and no tests for complexity yet. The sample solution for tests is `samples/solution01/SilkHat.Sample.sln`, with complexity examples in `samples/solution01/SilkHat.Sample.App/AnalysisSamples.cs` (class `ComplexitySamples`).

A “DocID” is the Roslyn documentation identifier string (for example `M:SilkHat.Sample.App.ComplexitySamples.CalculateScore(System.Int32)`) that can be used to find the symbol across compilations. A “concrete type” here means a class, struct, or record (not an interface). A “method” here means an ordinary method or a constructor; property accessors and event accessors are not included unless explicitly stated.

## Plan of Work

First, add complexity models and strategy interfaces. In `src/SilkHat.Code.Core/Dtos/ComplexityDtos.cs`, define enums `ComplexityMeasureType` (Cognitive, Cyclomatic, Indentation) and `ComplexityTargetKind` (Method, NamedType). Define `ComplexityResultDto` with `DocumentationId`, `MeasureType`, `TargetKind`, `TargetTypeKind` (nullable `NamedTypeKind` for class/struct/record), and `Value`. Keep this DTO simple, as the API should return it directly.

In `src/SilkHat.Code.Analysis/Abstractions`, create:

- `IComplexityStrategy` with a method that takes a method syntax node and semantic model and returns an integer complexity value.
- `IComplexityStrategyFactory` that returns a strategy given `ComplexityMeasureType`.
- `IMethodComplexityService` with `Task<ComplexityResultDto?> GetMethodComplexityAsync(CodeSolutionWorkspace solution, string docId, ComplexityMeasureType measure, CancellationToken cancellationToken)`.
- `ITypeComplexityService` with `Task<ComplexityResultDto?> GetTypeComplexityAsync(CodeSolutionWorkspace solution, string docId, ComplexityMeasureType measure, CancellationToken cancellationToken)`.

Implement the strategy classes under `src/SilkHat.Code.Analysis/Services/Complexity` (create the folder). Implement the factory under `src/SilkHat.Code.Analysis/Services/Complexity/ComplexityStrategyFactory.cs`. Each strategy should focus on a single complexity algorithm and be testable in isolation.

Define the complexity algorithms in plain, deterministic terms:

- Cyclomatic complexity starts at 1 and increments by 1 for each decision point. Decision points are `if`, `else if`, `for`, `foreach`, `while`, `do`, `catch`, each `case` in a `switch` (exclude `default`), and each ternary `?:`. Additionally, each `&&` or `||` inside a conditional expression increments by 1. Implement this using a `CSharpSyntaxWalker` that inspects `BinaryExpressionSyntax` for logical operators and counts the listed statement kinds. Do not count method calls or `switch` itself, only its cases.

- Cognitive complexity starts at 0 and increments by 1 for each control flow statement, plus an extra increment for each level of nesting at the point of the statement. Control flow statements include `if`, `else if`, `for`, `foreach`, `while`, `do`, `switch`, `catch`, and ternary `?:`. Additionally, each `&&` or `||` inside a condition increments by 1 (no extra nesting). Nesting depth increases when entering the body of a control flow statement and decreases when exiting. Implement this with a walker that tracks current nesting depth and applies the “1 + depth” rule when it sees a control flow node.

- Indentation complexity is computed from the method’s source lines. Determine the indentation of the method declaration line by counting leading spaces and tabs (tabs count as 4 spaces). For each non-empty line within the method’s full line span, count leading whitespace in the same way, subtract the method declaration indentation (do not go below 0), and sum the normalized values. Include the method declaration line and the body lines; ignore blank lines (lines that are empty or whitespace-only) by contributing 0. This metric uses syntax location line spans from the method’s syntax node and the source text from the syntax tree.

Implement method complexity by resolving the DocID to an `IMethodSymbol` using `DocumentationIdUtility.FindMethodByDocumentationId`. Use `DeclaringSyntaxReferences` to find the method syntax node; if no syntax reference exists in the current solution, return null (API will map this to 404). If the method has an expression body, treat it as a method with a single line body; the walkers should handle it naturally. For constructors, treat them as methods using the same approach. Ignore methods in metadata-only assemblies.

Implement type complexity by resolving the DocID to an `INamedTypeSymbol` using `DocumentationIdUtility.FindTypeByDocumentationId`. Verify the type is a concrete type (class, struct, record). Collect its methods that have source syntax in the solution and `MethodKind` is `Ordinary`, `Constructor`, or `StaticConstructor`. For each method, compute complexity using the selected strategy and sum them. The result should return `TargetKind = NamedType` and `TargetTypeKind` set to the named type kind. If the named type DocID resolves to an interface, return null so the API can return a 400 with a clear message. If the type has no methods with source, return a result with value 0.

Add services in `src/SilkHat.Code.Analysis/Services`:

- `MethodComplexityService` implementing `IMethodComplexityService`.
- `TypeComplexityService` implementing `ITypeComplexityService`.

Both services should accept `IComplexityStrategyFactory` and use it for the selected measure. This keeps the “strategy” requirement explicit and testable.

Add API endpoints. Use thin controllers and query parameters for DocID and measure. Add the following endpoints:

- In `src/SilkHat.Api/Controllers/CodeMethodsController.cs` (new controller), add `GET /api/repositories/{id}/code/solutions/{solutionId}/methods/complexity?docId=...&measure=...` returning `ComplexityResultDto`. This controller should mirror the pattern used in `MethodCallStackController`: resolve workspace and solution, call the method complexity service, and return ProblemDetails on not-found or invalid inputs. Use the existing `ApiControllerBase.ProblemWithCategory` helper.
- In `src/SilkHat.Api/Controllers/CodeNamedTypesController.cs`, add `GET /api/repositories/{id}/code/solutions/{solutionId}/named-types/complexity?docId=...&measure=...` returning `ComplexityResultDto`. This should route for classes/structs/records. If the DocID resolves to an interface, return 400 with a message that only concrete types are supported.

Wire services in `src/SilkHat.Api/Program.cs`:

- Register `IComplexityStrategyFactory`, `IMethodComplexityService`, `ITypeComplexityService`, and each strategy if they are injected directly.

Do not modify existing tests except to wire new dependencies into existing test setups if required. Any new tests must be additive.

## Concrete Steps

1) Create DTOs and enums.
   - Add `src/SilkHat.Code.Core/Dtos/ComplexityDtos.cs` with enums and `ComplexityResultDto`.
   - Ensure `SilkHat.Code.Core` is referenced where needed.

2) Add strategy interfaces and factory.
   - Create interfaces in `src/SilkHat.Code.Analysis/Abstractions`.
   - Add strategy implementations in `src/SilkHat.Code.Analysis/Services/Complexity`.

3) Implement `MethodComplexityService` and `TypeComplexityService` in `src/SilkHat.Code.Analysis/Services`.

4) Add API controller(s) and wire DI.
   - New `CodeMethodsController` and updates to `CodeNamedTypesController`.
   - Update `src/SilkHat.Api/Program.cs` DI registrations.

5) Add tests.
   - Strategy unit tests in `tests/SilkHat.Tests/Services/ComplexityStrategyTests.cs` (or split by strategy).
   - Service tests in `tests/SilkHat.Tests/Services/MethodComplexityServiceTests.cs` and `TypeComplexityServiceTests.cs` using the sample solution loader pattern already present in tests (see `SymbolDescriptionServiceTests` for locating `samples/solution01/SilkHat.Sample.sln`).
   - API controller tests in `tests/SilkHat.Api.Tests/Controllers/CodeMethodsControllerTests.cs` and new cases in `CodeNamedTypesControllerTests.cs` to verify responses and dependency calls.

6) Run validation commands from repo root:

   - `dotnet test`
   - If needed for milestone verification, `REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose up --build -d` then `REPO_ROOT=/home/vbfg/repos/c/dev/repos/ docker-compose down`

## Validation and Acceptance

- Running `dotnet test` succeeds.
- For a known DocID (e.g., `M:SilkHat.Sample.App.ComplexitySamples.CalculateScore(System.Int32)`), the method complexity endpoint returns HTTP 200 with the expected integer value for each measure and `TargetKind = Method`.
- For the DocID of `SilkHat.Sample.App.ComplexitySamples` type, the type complexity endpoint returns HTTP 200 with the sum of method complexities and `TargetKind = NamedType` and `TargetTypeKind = Class`.
- Requests with missing DocID or unknown DocID return ProblemDetails with an informative message and a 400/404 status.
- The new tests fail before the change and pass after.

## Idempotence and Recovery

All changes are additive. If a test fails due to expected values, adjust the expected values in the new tests only after verifying the algorithm matches the stated rules. Rerun `dotnet test` until passing. If an API endpoint signature causes binding issues, prefer query parameters for DocID and measure to avoid routing problems.

## Artifacts and Notes

Example DocIDs from the sample solution (for tests and manual validation):

    M:SilkHat.Sample.App.ComplexitySamples.CalculateScore(System.Int32)
    T:SilkHat.Sample.App.ComplexitySamples

Example API call shape (replace ids):

    GET /api/repositories/{repoId}/code/solutions/{solutionId}/methods/complexity?docId=M%3ASilkHat.Sample.App.ComplexitySamples.CalculateScore(System.Int32)&measure=Cyclomatic

## Interfaces and Dependencies

In `src/SilkHat.Code.Analysis/Abstractions`, define:

    public interface IComplexityStrategy
    {
        ComplexityMeasureType MeasureType { get; }
        int Compute(BaseMethodDeclarationSyntax method, SemanticModel semanticModel, SourceText sourceText);
    }

    public interface IComplexityStrategyFactory
    {
        IComplexityStrategy GetStrategy(ComplexityMeasureType measureType);
    }

    public interface IMethodComplexityService
    {
        Task<ComplexityResultDto?> GetMethodComplexityAsync(
            CodeSolutionWorkspace solution,
            string docId,
            ComplexityMeasureType measure,
            CancellationToken cancellationToken);
    }

    public interface ITypeComplexityService
    {
        Task<ComplexityResultDto?> GetTypeComplexityAsync(
            CodeSolutionWorkspace solution,
            string docId,
            ComplexityMeasureType measure,
            CancellationToken cancellationToken);
    }

In `src/SilkHat.Code.Core/Dtos/ComplexityDtos.cs`, define:

    public enum ComplexityMeasureType { Cognitive = 1, Cyclomatic = 2, Indentation = 3 }
    public enum ComplexityTargetKind { Method = 1, NamedType = 2 }
    public sealed record ComplexityResultDto(
        string DocumentationId,
        ComplexityMeasureType MeasureType,
        ComplexityTargetKind TargetKind,
        NamedTypeKind? TargetTypeKind,
        int Value);

## Test Plan

Add unit tests that compute expected values for all three measures against `ComplexitySamples.CalculateScore`. Use the sample solution loader pattern already in `tests/SilkHat.Tests/Services/SymbolDescriptionServiceTests.cs` to locate and parse `samples/solution01/SilkHat.Sample.sln`. Tests must verify:

- Each strategy returns the expected integer value for the same method.
- The factory returns a strategy matching the requested measure.
- `MethodComplexityService` returns a `ComplexityResultDto` with correct metadata and value.
- `TypeComplexityService` sums all method complexities for `ComplexitySamples` and returns a `ComplexityResultDto` with `TargetKind = NamedType` and correct `TargetTypeKind`.

Add API controller tests that verify:

- The methods complexity endpoint returns 200 and the expected value when the service returns a result.
- The named type complexity endpoint returns 200 for class DocIDs and 400 for interface DocIDs.
- Missing DocID yields ProblemDetails.

Do not modify existing tests except to add or update dependency wiring when a new constructor parameter is required by a controller or service.

Plan Update Notes: 2026-01-09 — Initial M14 ExecPlan drafted to cover complexity strategies, services, and API endpoints with DocID-first behavior.
Plan Update Notes: 2026-01-09 — Updated interfaces and progress to use BaseMethodDeclarationSyntax for constructor support and to record completed implementation/testing steps.
Plan Update Notes: 2026-01-09 — Recorded record-struct classification adjustment and test alignment.
