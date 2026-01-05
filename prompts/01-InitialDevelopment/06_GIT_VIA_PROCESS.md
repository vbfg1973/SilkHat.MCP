# Git via Process (No LibGit2Sharp)

Implement in SilkHat.Git.Analysis:
- IGitCommandRunner:
  - Execute(repoRoot, args[], cancellationToken) -> stdout/stderr/exitCode
- GitCli:
  - Thin wrapper with methods:
    - ListTree(...)
    - FileHistory(...)
    - CoChangeStats(...)

Rules:
- Always run git with WorkingDirectory = repoRoot
- Prefer stable output formats:
  - Use `git log --name-status --date=iso-strict --pretty=format:...`
  - Use `git show --numstat --format=...`
  - Use `git ls-tree -r --name-only HEAD` for tree
  - For last change per file:
    - `git log -n 1 --date=iso-strict --pretty=format:... -- <path>`
- Parse outputs with robust, tested parsers.
- Normalize returned paths to "./..."

Performance:
- Add caching per LoadedRepository for:
  - path -> last change metadata
  - commit -> changed files map (for co-change queries)
- Be careful: caches must be invalidated on reload.

Degrade gracefully:
- If git missing or repo not a git repo, return ProblemDetails with category=Git.
