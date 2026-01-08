M7: Git Commit Query + Paging + IDE Commit Metadata/Diff

Summary:
- add paged list contracts across list endpoints and update API clients/tests
- implement git commit query filters (author/sha/date/merge/file changes) with change-type enums and coverage
- add git file last-change metadata + diff endpoint and UI rendering, including MudCard commit details + diff toggle
- fix IDE tab rendering refresh and align UI metadata display with short SHA and author email
- align Serilog browser console package to resolved version

Key files:
- src/SilkHat.Core/Dtos/PagingDtos.cs
- src/SilkHat.Api/Extensions/PagingExtensions.cs
- src/SilkHat.Api/Controllers/GitCommitsController.cs
- src/SilkHat.Api/Controllers/GitFilesController.cs
- src/SilkHat.Git.Analysis/Services/GitCli.cs
- src/SilkHat.Git.Core/Dtos/GitFileDiffDtos.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Models/GitModels.cs
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Ui/SilkHat.Ui.csproj
- tests/SilkHat.Tests/Services/GitCliTests.cs
- tests/SilkHat.Api.Tests/Controllers/GitFilesControllerTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs
- prompts/01-InitialDevelopment/state/EXECPLAN_M7.md

Notes:
- commit metadata card now shows short SHA, timestamp (UTC), author, and author email alongside diff toggle
- diff overlay uses line-based annotations to insert deleted lines and highlight added lines

Tests:
- dotnet test (user confirmed)

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d (user confirmed)
