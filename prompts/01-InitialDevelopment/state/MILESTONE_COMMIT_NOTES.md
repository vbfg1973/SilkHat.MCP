M1: skeleton solution, API health, MudBlazor UI, Docker compose

Summary:
- Create .NET solution with all required /src and /tests projects.
- Implement API /api/health with Swagger enabled.
- Replace Blazor WASM template with MudBlazor layout, persisted theme toggle, and health check display.
- Add Dockerfiles for API/UI, nginx proxy for /api, root docker-compose.yml, and .dockerignore.
- Add minimal /docs/README.md with local and Docker run instructions.

Key files:
- SilkHat.sln
- src/SilkHat.Api/Program.cs
- src/SilkHat.Api/Dockerfile
- src/SilkHat.Ui/Program.cs
- src/SilkHat.Ui/Layout/MainLayout.razor
- src/SilkHat.Ui/Pages/Home.razor
- src/SilkHat.Ui/Dockerfile
- src/SilkHat.Ui/nginx.conf
- docker-compose.yml
- .dockerignore
- docs/README.md

Notes:
- Targeted net9.0 due to installed SDK (9.0.112) with intent to bump to net10.0 later.

Tests:
- DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false

Docker:
- docker compose up --build -d
