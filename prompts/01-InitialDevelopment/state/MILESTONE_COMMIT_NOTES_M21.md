M21: real package/git/complexity indexing into the graph

- Added package indexing during solution load: parse PackageReferences, create Package/PackageVersion nodes, and Project→PackageVersion/PackageVersion→Package edges with versions (fallback “unspecified”).
- Added git indexing from a single `git log --numstat` pass per repo: GitAuthor/GitCommit nodes, Commit→Author/Commit→File/Commit→Solution edges, plus file-level change/add/delete and distinct-author aggregates.
- Added complexity indexing using strategy factory: per-method metrics (cognitive/cyclomatic/indentation) stored as ComplexityMetric nodes with HasMetric edges; rolled up to types.
- Graph store deduplicates edges by (source, target, type); status store now reports real package/git/complexity job progress.
- Base load now finishes after projects/files/types; packages/git/complexity run as deferred background jobs with status updates so the IDE can render immediately.
- Tests: `dotnet test -m:1` (SilkHat.Tests, SilkHat.Api.Tests, SilkHat.Ui.Tests) all passing. Existing nullable warning remains in `src/SilkHat.Ui/Components/Ide/IdeTabs.razor:229`.
