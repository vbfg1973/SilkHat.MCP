M20: planned — QuikGraph-backed solution index with job orchestration and status polling

Plan highlights:
- Build in-memory QuikGraph index per solution load (projects/files, types/members, packages, git commits/authors, complexity metrics) with typed edges.
- Run dependency-aware jobs (projects/files; types/members; packages; git; complexity) with per-job status/progress.
- Expose index status API; UI Status dialog polls every second during solution load while jobs are running and stops when complete.
- Keep existing API shapes (tree, symbols, annotations) and re-source them from the graph.
- Graph edges carry EdgeType (e.g., Contains, DeclaresType/Member, Inherits, Implements, Calls, Changes, AuthoredBy, DependsOnPackage, ExternalReference, HasMetric, PropertyType, FieldType, ReturnType, ParameterType) via serialization-friendly edge DTOs; method/constructor metadata captures parameters (name, type DocId/SymbolKey, ordinal, optional) with edges to parameter types.

Status: Documentation and execution plan created; implementation to follow in M20.
