# M20: QuikGraph-backed solution index with job orchestration and status polling

This ExecPlan is a living document. Maintain it per `.agents/PLANS.md`. Keep `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` current.

## Purpose / Big Picture

Replace ad-hoc indexing with a rich in-memory graph (QuikGraph) built at solution load. All existing APIs (tree, symbols, complexity/git annotations) should read from this graph without breaking contracts. Indexing runs as typed jobs with progress/status exposed via API; the UI polls status after a solution load while jobs are running and stops polling when all jobs complete. Design for future serialization but keep this milestone in-memory.

## Progress

- [x] Draft graph model (nodes/edges) and job orchestration design (graph model settled; orchestration still pending).
- [x] Implement graph container and lookup indices (GraphStore + GraphQueryService with keyed lookups and in/out edges).
- [ ] Implement indexing jobs (projects/files; types/members; packages; git; complexity) with status reporting. *Partial*: workspace loader now builds graph nodes/edges for solution/project/folder/file/type/member/parameter with locations; other jobs + status remain.
- [ ] Expose status endpoint and wire UI polling dialog (start on solution load if jobs running; stop on completion).
- [ ] Adapt existing services (tree, symbols, annotations) to read from the graph. *Partial*: CodeTreeService now reads from graph (members included); CodeSymbolsController is graph-first with legacy fallback; metrics/filtering still legacy.
- [ ] Tests (unit/API/UI) and docs. *Partial*: all suites currently passing after graph/tree/symbol changes; docs not yet updated.

## Surprises & Discoveries

- Graph member nodes (fields/properties/methods/events/parameters) plus location/project data allow tree queries without outline fallback; outline fallback retained as temporary safety.
- Symbol lookup works via graph docId/symbolKey; legacy maps kept as fallback until full migration.

## Decision Log

- Graph engine: QuikGraph in-memory; keep payloads serializable for future persistence.
- Status polling: UI starts polling when a solution load begins and any index job is running; stops when all jobs reach completed/failed.
- Keep existing API shapes stable; swap internals to graph-backed data.
- Type nodes now include namespace/assembly; nodes carry project metadata and locations.
- GraphQueryService exposes keyed node lookup and neighbor traversal; controllers/services should use it going forward.

## Outcomes & Retrospective

To be filled after delivery.

## Scope / Non-goals

In scope:
- In-memory QuikGraph index built per solution load.
- Job orchestration with per-job status/progress.
- Status API and UI polling tied to solution load lifecycle.
- Graph-backed read paths for existing APIs (tree, symbols, annotations) without changing contracts.

Out of scope (future):
- Persistent serialization/storage of the graph.
- New analyses beyond current domains; additional analyses may publish to graph later.

## Graph Model

Nodes (typed):
- Solution, Project, Package, PackageVersion.
- GitAuthor, GitCommit.
- Folder, File.
- NamedType (class/struct/record/record struct/interface/enum).
- Field, Property, Method.
- ComplexityMetric (per type/method, kind/value).

Relationships (directional examples):
- Solution → Project; Project → Folder; Folder → Folder; Folder → File; Project → Package; Package → PackageVersion.
- File → NamedType; NamedType → NamedType (inherits/implements); NamedType → Method/Property/Field.
- Property → NamedType (property type); Field → NamedType (field type); Method → NamedType (return type); (parameters later if needed).
- Method → Method (implements interface method); Method → Method (calls) [optional later].
- GitCommit → File (changes); GitCommit → GitAuthor (author); File → GitCommit (changed by).
- NamedType → PackageVersion (external type reference) for external resolutions.
- ComplexityMetric → NamedType/Method.

Identity: use DocId/SymbolKey for code, repo-relative paths for files/folders, project key, commit SHA, package id/version; keep payloads serializable.

## Indexing Pipeline

Trigger: on solution load per workspace.

Jobs (dependency-aware, parallel where possible):
1) Projects/Folders/Files (Roslyn solution) → nodes/edges.
2) Types/Members (Roslyn symbols) → nodes/edges (inherit/implement, members).
3) Packages (project assets) → Package/PackageVersion nodes/edges.
4) Git metrics (single `git log --numstat`) → GitAuthor/GitCommit/File edges + change/author data.
5) Complexity metrics (existing strategies) → ComplexityMetric nodes/edges on types/methods.

Each job reports status: state (pending/running/completed/failed), percent, message. Stored per solution. Jobs invalidate/rebuild on unload/reload.

## API Surface

- Keep existing endpoints; internally source from graph:
  - Repo tree from graph nodes (Solution/Project/Folder/File/Type/Member) honoring filters/annotations.
  - Symbol lookups from graph by DocId/SymbolKey.
  - Complexity/git annotations from graph-backed metrics.
- New status endpoint: `GET /api/repositories/{id}/code/solutions/{solutionId}/index/status` → per-job status.
- Optional rebuild endpoint if needed: `POST /api/repositories/{id}/code/solutions/{solutionId}/index/rebuild`.

## UI

- Add a Status dialog (from File menu) that polls the status endpoint every 1s while open.
- Polling starts when a solution load begins and any index job is running; stops when all jobs complete/failed or dialog closes.
- Display per-job state/progress.

## Implementation Steps

1) Graph scaffolding: define node/edge types, IDs, graph container, and lookup dictionaries for fast queries.
   - DONE for container/lookups and file/type/member/parameter nodes with locations.
2) Job orchestration: job base + state model + orchestrator; implement job workers (projects/files; types/members; packages; git; complexity).
3) Status API: controller + models returning per-job status; hook into orchestrator.
4) UI: Status dialog component + polling tied to solution load; surfaces job state/progress.
5) Service adaptation: back tree/symbol/annotation services with graph queries.
   - PARTIAL: tree service graph-backed; symbol lookup graph-first; metrics/filters still legacy.
6) Tests:
   - Unit: graph builders per job, inheritance/implementation edges, distinct author rollups, complexity attachment.
   - API: status endpoint; regression on tree/symbol endpoints to ensure unchanged behavior.
   - UI: status dialog polling with stubbed service.

## Validation / Acceptance

- Loading a solution triggers indexing jobs; status endpoint shows running jobs with progress.
- When jobs finish, status shows completed/failed; UI polling stops automatically.
- Existing APIs (tree, symbols, annotations) behave as before, now backed by graph data.
- `dotnet test` passes; manual check via Status dialog shows live job progress.

Plan Update Notes: 2026-01-10 15:08Z — Initial plan for M20 (graph index + job orchestration + UI polling).
Plan Update Notes: 2026-01-10 15:13Z — Added typed edges, serialization-friendly edge DTOs, and method/constructor parameters with ordinal.
Plan Update Notes: 2026-01-17 15:58Z — Graph-backed tree/symbol lookup implemented; graph now includes members/parameters/locations/project metadata. Status/metrics/filter migration and job orchestration still pending.

## Graph Extensions (Edge Types + Method Parameters)

- **Edge types:** Use an `EdgeType` enum (e.g., Contains, DeclaresType, DeclaresMember, Inherits, Implements, ImplementsMember, Calls, Changes, AuthoredBy, DependsOnPackage, ExternalReference, HasMetric, PropertyType, FieldType, ReturnType, ParameterType) and store it on edges to enable filtered degree/traversal. Edges are serialized as DTOs: `{ SourceId, TargetId, EdgeType, Payload? }`.
- **Indices:** Maintain adjacency/lookup indices keyed by `(nodeId, edgeType)` to query in/out-degree by edge type and to traverse only relevant relationships.
- **Method/Constructor parameters:** Capture parameters as part of method metadata (name, type DocId/SymbolKey, isOptional, ordinal/position) and expose parameter edges to parameter types (EdgeType.ParameterType) with the ordinal preserved.
