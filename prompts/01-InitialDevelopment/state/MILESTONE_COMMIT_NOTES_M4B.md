M4b: UI hosting fixes and component testing

Summary:
- Fixed MudThemeProvider usage and added MudPopoverProvider so the UI renders without runtime exceptions.
- Corrected MudSelectItem generic types for nullable group selection.
- Added a new bUnit UI test project with layout and dashboard coverage using fake HttpClient and JSInterop.
- Documented the UI component testing standard and enforced it in the definition of done.

Key files:
- src/SilkHat.Ui/Layout/MainLayout.razor
- src/SilkHat.Ui/Pages/Home.razor
- tests/SilkHat.Ui.Tests/SilkHat.Ui.Tests.csproj
- tests/SilkHat.Ui.Tests/Layout/MainLayoutTests.cs
- tests/SilkHat.Ui.Tests/Pages/HomeTests.cs
- tests/SilkHat.Ui.Tests/TestHelpers/FakeHttpMessageHandler.cs
- prompts/01-InitialDevelopment/11_TESTING_STRATEGY.md
- prompts/01-InitialDevelopment/13_DEFINITION_OF_DONE.md
- prompts/01-InitialDevelopment/state/EXECPLAN_M4B.md

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
