M3: command streaming + repository load orchestration

Summary:
- Add repo command abstractions, in-memory loaded repository store, and channel-based command processor.
- Implement load command emitting NDJSON-friendly progress/completed events.
- Add controller endpoints for /load (NDJSON stream) and /unload.
- Update UI dashboard to initiate loads and render live progress events.
- Ensure Docker builds include Analysis project and UI list typing.

Key files:
- src/SilkHat.Analysis/Abstractions/IRepoCommand.cs
- src/SilkHat.Analysis/Services/RepoCommandProcessor.cs
- src/SilkHat.Analysis/Commands/LoadRepositoryCommand.cs
- src/SilkHat.Core/Dtos/RepoEvents.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
- src/SilkHat.Api/Program.cs
- src/SilkHat.Ui/Services/RepositoryApiClient.cs
- src/SilkHat.Ui/Pages/Home.razor
- src/SilkHat.Api/Dockerfile

Notes:
- Load endpoint streams `application/x-ndjson` with one JSON object per line.
- Loaded repository state is in-memory and serialized per repo via SemaphoreSlim.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
