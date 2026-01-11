# M21: Real indexing jobs (packages, git, complexity) with phased delivery

This ExecPlan is a living document. Maintain it per `.agents/PLANS.md`. Keep `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` current. Include phases; each phase must end with tests passing and behaviour verified.

## Purpose / Big Picture

Replace placeholder status updates with real indexing work during solution load: packages, git, and complexity. Emit full graph nodes/edges for these domains so downstream services (tree, metrics, annotations, decisions) rely solely on the graph. Do this in phases, with passing tests at each phase.

## Phases

1) Packages
   - Parse PackageReferences/assets and emit Package + PackageVersion nodes.
   - Edges: Project —DependsOnPackage→ PackageVersion; PackageVersion —IsVersionOf→ Package.
   - Attributes: package id, version.
   - Update status store to mark Packages job Running/Completed/Failed based on real work.

2) Git
   - Run single `git log --numstat` per repo (reuse GitCli) during load.
   - Create GitAuthor, GitCommit nodes; Repository node if needed for linkage.
   - Edges: GitCommit —AuthoredBy→ GitAuthor; GitCommit —BelongsToRepository→ Repository; GitCommit —TouchesFile→ File.
   - Aggregate file change/author counts into File nodes (attributes) for fast lookup.
   - Update status store accordingly.

3) Complexity
   - Reuse existing complexity strategies/aggregator to compute per-method and per-type metrics during load.
   - Nodes: ComplexityMetric; Edges: Method —HasComplexity→ ComplexityMetric; Type —HasComplexity→ ComplexityMetric (sum of methods).
   - Attributes: MeasureType, Value, TargetDocId.
   - Update status store accordingly.

4) Stabilization
   - Ensure tree/metrics/annotations use only graph data for these domains (no placeholders).
   - Update docs/architecture notes on graph triples for packages/git/complexity.
   - Smoke (docker) and full test run.

## Progress

- [x] Phase 1: Packages
- [x] Phase 2: Git
- [x] Phase 3: Complexity
- [x] Phase 4: Stabilization (docs/tests/smoke)

## Surprises & Discoveries
- `git log --numstat` parsing is fast enough when run once per repo; everything else hangs off the graph without extra queries.
- Complexity calculation needed a factory injection to avoid nulls during load.

## Decision Log
- Use semantic triples as edge contracts; edge types must be recorded and documented.
- One pass per repo for git (`git log --numstat`) to avoid repeated process cost.
- Aggregate file-level metrics (changes/authors) into File node attributes for speed; still retain commit/author edges for traversal.

## Outcomes & Retrospective
- Graph now owns packages, git authors/commits/changes, and complexity metrics; tree/annotations use graph data only.
- Status store reflects real work for packages/git/complexity jobs.
- All unit tests pass (`dotnet test -m:1` per suite); one existing nullable warning remains in `IdeTabs.razor`.
- Base load stops after projects/files/types; expensive package/git/complexity indexing runs in deferred background jobs with status updates so the IDE can appear quickly.
