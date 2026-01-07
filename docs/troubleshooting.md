# Troubleshooting

## Docker compose fails with REPO_ROOT missing

Set `REPO_ROOT` to a host path before running:

- `export REPO_ROOT=/path/to/repos`
- `./scripts/docker-up.sh`

## Docker daemon permission denied

Ensure your user has access to the Docker socket or run Docker Desktop with the correct permissions.

## pgAdmin login fails

Use the configured defaults from `docker-compose.yml`:
- Email: `admin@example.com`
- Password: `admin`

If pgAdmin fails to start due to invalid email, ensure the email contains a valid domain.

## dotnet test fails with MSBuild named pipe permission denied

This can occur in restricted environments. Run tests in a local environment with standard OS pipe permissions.

## UI cannot reach API

Verify the UI is configured to point at the API host and port (`http://localhost:18080`). If running locally without Docker, check CORS settings in `src/SilkHat.Api/appsettings.json`.
