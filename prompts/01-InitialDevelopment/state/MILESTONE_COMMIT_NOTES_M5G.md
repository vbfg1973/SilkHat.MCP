M5g: IDE file tabs + fast file content

Summary:
- add file content DTO, service, validator, and controller for solution-scoped file reads
- update IDE to open file tabs, allow closing individual tabs, and add close-all action
- remove file history panel and allow solution switching across all repositories while preserving per-solution view state
- adjust layout to a fixed-width tree with a flex-growing tab panel within 90% viewport width
- make the main navigation drawer collapsible to reclaim horizontal space
- add API/service/UI tests for file content retrieval, validation, and tab behavior

Key files:
- src/SilkHat.Api/Controllers/CodeFilesController.cs
- src/SilkHat.Api/Models/CodeFileQuery.cs
- src/SilkHat.Api/Validation/CodeFileQueryValidator.cs
- src/SilkHat.Api/Program.cs
- src/SilkHat.Code.Analysis/Abstractions/ICodeFileService.cs
- src/SilkHat.Code.Analysis/Models/CodeFileContentResult.cs
- src/SilkHat.Code.Analysis/Services/CodeFileService.cs
- src/SilkHat.Code.Core/Dtos/CodeFileContentDto.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Layout/MainLayout.razor
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Ui/Models/CodeFileContentModel.cs
- tests/SilkHat.Api.Tests/Controllers/CodeFilesControllerTests.cs
- tests/SilkHat.Api.Tests/Validation/CodeFileQueryValidatorTests.cs
- tests/SilkHat.Tests/Services/CodeFileServiceTests.cs
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs
- prompts/01-InitialDevelopment/03_MILESTONES.md
- prompts/01-InitialDevelopment/08_API_SURFACE.md
- prompts/01-InitialDevelopment/state/EXECPLAN_M5G.md

Notes:
- file contents are resolved from repository root using tree entry display paths and validated against path traversal
- IDE keeps open tabs/tree expansion per solution when switching across repositories
- syntax highlighting remains plain monospaced text due to no easy MudBlazor highlighter component

Tests:
- dotnet test (pass)

Docker:
- docker compose up --build -d (API + UI running)
