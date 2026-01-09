M16: add GitHub Actions pipeline for tests, integration gates, and GHCR publishing

- run unit tests on every branch push and integration tests on develop
- publish tagged API/UI images to GHCR only from master after tests pass
- implement automatic version tag generation (1.0.<minor>) with 1.0.1 as the first tag
- document release pipeline and tag/image naming in operations guide
