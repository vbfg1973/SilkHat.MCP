M20: QuikGraph-backed solution index with job orchestration and status polling

What we delivered
- Graph store + query service with keyed lookups and typed edges; members/parameters carry locations and project metadata.
- Workspace loader builds graph nodes/edges for solution → project → folder/file → type → member/parameter and return/value/parameter type links; edges are de-duped by (source, target, edge type).
- CodeTreeService is graph-only; tree filtering/annotations use graph metrics; CodeSymbolsController resolves symbols graph-first (docId/symbolKey) with legacy map fallback for compatibility. Tree roots are de-duplicated and member ordering is deterministic (fields → properties → methods).
- Index job status plumbing: in-memory status store, loader hooks, new API `GET /api/repositories/{repoId}/code/solutions/{solutionId}/index/status`, and Blazor Status dialog with auto-polling during solution load and manual launch from the File menu.
- UX polish: type/member clicks open the owning file, highlight full spans, and auto-scroll; Mermaid/tabbed views load quickly with graph-backed data.
- All test suites passing; docker smoke verified.

Notes / follow-on
- Package/git/complexity job slots are registered and reported complete with placeholder messages; M21 will add full implementations.
- Docs/architecture updated for graph-first flows and status UX.

Notes for next milestone (M21)
- Implement real indexing jobs for packages, git, complexity (phased) and wire them into the status pipeline/graph.
