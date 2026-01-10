M19: repository tree annotations, filtering, and symbol expansion

- add server-side tree annotation/filtering with cached precomputed metrics
- expand tree nodes to show file types and member symbols with complexity badges
- enable method/constructor selection to open files and highlight full method ranges
- harden tree UI scrollability, spacing, and badge layout under expansion
- fix file author aggregation to roll up distinct authors for folders/projects
- expand API/UI/tests to cover annotated trees, filtering, and author rollups

Tests:
- dotnet test -m:1
