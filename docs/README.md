# SilkHat (M1)

SilkHat is a .NET-based analyzer for dotnet repositories. This initial milestone sets up the solution layout,
an API health endpoint, and a Blazor WASM UI wired to the health check.

## Local

- API: `dotnet run --project src/SilkHat.Api/SilkHat.Api.csproj`
- UI: `dotnet run --project src/SilkHat.Ui/SilkHat.Ui.csproj`

## Docker

- `docker compose up --build`
- API: `http://localhost:5000/api/health`
- UI: `http://localhost:5001`
- SQLite DB (docker): persisted in `silkhat-data` volume at `/data/silkhat.db` inside the API container.
