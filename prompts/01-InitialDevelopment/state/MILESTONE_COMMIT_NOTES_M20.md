M20: in-flight — QuikGraph-backed solution index with job orchestration and status polling

What’s implemented so far
- Graph store + query service with keyed lookups and neighbor traversal.
- Workspace loader now builds graph nodes/edges for solution → project → folder/file, file → type, type → member/parameter (with locations, project metadata, namespaces/assemblies, return/value type links).
- CodeTreeService is graph-only (outline fallback removed); tree filtering/annotations use graph metrics; CodeSymbolsController resolves symbols graph-first (docId/symbolKey) with legacy map fallback for compatibility.
- Tests updated and all suites passing; docker build/smoke verified.

Still to do in M20
- Job orchestration + status endpoints + UI polling.
- Update docs once the above lands.

Notes for next milestone (M21)
- Implement real indexing jobs for packages, git, complexity (phased).
