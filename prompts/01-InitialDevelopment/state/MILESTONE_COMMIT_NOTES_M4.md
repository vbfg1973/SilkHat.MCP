M4: Buildalyzer + Roslyn code analysis endpoints and IDE scaffold

Summary:
- Load repository solutions with Buildalyzer into Roslyn workspaces and build code indices.
- Add code navigation endpoints (projects, references, namespaces, named types, symbol lookup).
- Extend repository load to build code analysis state and clear it on unload.
- Add IDE-like UI scaffold with project list, tabs placeholder, and context panel.
- Add SymbolKey reflection helper and required analysis packages.

Key files:
- src/SilkHat.Code.Analysis/Services/CodeWorkspaceLoader.cs
- src/SilkHat.Code.Analysis/Services/CodeWorkspaceStore.cs
- src/SilkHat.Code.Analysis/Services/SymbolKeyUtility.cs
- src/SilkHat.Code.Core/Dtos/CodeProjectDto.cs
- src/SilkHat.Code.Core/Dtos/NamedTypeDtos.cs
- src/SilkHat.Api/Controllers/CodeController.cs
- src/SilkHat.Api/Controllers/RepositoryLoadController.cs
- src/SilkHat.Api/Program.cs
- src/SilkHat.Ui/Pages/Ide.razor
- src/SilkHat.Ui/Layout/NavMenu.razor
- src/SilkHat.Api/Dockerfile

Notes:
- SymbolKey strings are generated via reflection because Roslyn SymbolKey APIs are internal.
- Solutions are discovered by scanning for `*.sln` files under the repo root.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
