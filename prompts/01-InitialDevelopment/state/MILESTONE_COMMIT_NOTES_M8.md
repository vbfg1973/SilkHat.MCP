M8: IDE Fluxor State (Phase 1) + Correlation-ID + UI Architecture Docs

Summary:
- migrate IDE page to Fluxor state (solutions/tree/tabs) with actions, reducers, effects, and feature registration
- split IDE into Fluxor components (solution selector, tree, tabs, commit card) and initialize store in App
- add correlation-id handler + logging for UI API calls; wire typed HttpClient
- document UI architecture state/actions/effects by domain; update testing approach for Fluxor components
- refine IDE tests with Fluxor helpers and ensure whitespace-preserving file rendering

Key files:
- src/SilkHat.Ui/Program.cs
- src/SilkHat.Ui/App.razor
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Components/Ide/IdeSolutionSelector.razor
- src/SilkHat.Ui/Components/Ide/IdeTree.razor
- src/SilkHat.Ui/Components/Ide/IdeTabs.razor
- src/SilkHat.Ui/Components/Ide/IdeCommitCard.razor
- src/SilkHat.Ui/State/Ide/Actions/*
- src/SilkHat.Ui/State/Ide/Effects/*
- src/SilkHat.Ui/State/Ide/Reducers/*
- src/SilkHat.Ui/State/Ide/Features/*
- src/SilkHat.Ui/Services/Http/CorrelationIdHandler.cs
- docs/architecture.md
- prompts/01-InitialDevelopment/state/EXECPLAN_M8.md
- tests/SilkHat.Ui.Tests/Pages/IdeTests.cs
- tests/SilkHat.Ui.Tests/TestHelpers/NoOpActionSubscriber.cs
- tests/SilkHat.Ui.Tests/TestHelpers/StateWrapper.cs

Notes:
- IDE file rendering preserves leading whitespace via pre-style span.
- Fluxor store initialization is required in App for effects to run.

Tests:
- dotnet test -m:1 (user confirmed)

Docker:
- REPO_ROOT=/home/vbfg/repos/c/dev/repos docker compose up --build -d (user confirmed)
