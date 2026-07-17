# Project-K self-host

Project-K supports two self-host friendly startup paths:

- local try-out from a cloned repository;
- Docker self-host from a GitHub Release bundle without git.

Frontend and backend stay separate in both modes. The frontend is an Angular app. The backend is an ASP.NET Core API. Azurite provides local Azure Blob-compatible storage.

## Local try-out from cloned repository

Use this when you have git, .NET SDK, Node.js, npm, SQL Server, and Azurite available on your machine.

Windows:

```powershell
./scripts/doctor.ps1
./scripts/start-local.ps1
```

Linux/macOS:

```bash
chmod +x ./scripts/*.sh
./scripts/doctor.sh
./scripts/start-local.sh
```

Default URLs:

- Frontend: http://localhost:4200
- Backend: http://localhost:5205
- Swagger: http://localhost:5205/swagger
- Azurite blob endpoint: http://127.0.0.1:10000/devstoreaccount1

Stop the local stack:

```powershell
./scripts/stop-local.ps1
```

or:

```bash
./scripts/stop-local.sh
```

Logs are written under `.tmp/local-run/logs`.

## Docker self-host without git

Use this when you want to run Project-K on a server or on a machine that has Docker but does not have git, .NET SDK, Node.js, or npm.

Download the `projectk-<version>-docker-selfhost.zip` or `.tar.gz` bundle from GitHub Releases.

Windows:

```powershell
Expand-Archive projectk-0.14.1-docker-selfhost.zip
cd projectk-0.14.1-docker-selfhost
copy .env.example .env
notepad .env
docker compose up -d
```

Linux/macOS:

```bash
tar -xzf projectk-0.14.1-docker-selfhost.tar.gz
cd projectk-0.14.1-docker-selfhost
cp .env.example .env
nano .env
docker compose up -d
```

At minimum, change these values before exposing the instance outside your machine:

- `PROJECTK_JWT_KEY`
- `PROJECTK_SQL_PASSWORD`
- `PROJECTK_DB_CONNECTION_STRING` password portion
- `PROJECTK_PUBLIC_URL`
- `PROJECTK_API_URL`
- `PROJECTK_CORS_ORIGIN`
- `PROJECTK_BLOB_PUBLIC_BASE_URL`

Default Docker URLs:

- Frontend: http://localhost:8080
- Backend: http://localhost:5205
- Backend health: http://localhost:5205/health
- Azurite blob endpoint: http://localhost:10000/devstoreaccount1

## Containers

The release bundle compose file uses published container images only and does not build from source. It starts:

- `projectk-web`: nginx serving the Angular build;
- `projectk-api`: ASP.NET Core API;
- `projectk-sql`: SQL Server;
- `projectk-azurite`: Azurite storage emulator.

The frontend image reads `PROJECTK_API_URL` at container startup and writes it into `env.js`, so you can reuse the same image across hosts without rebuilding it for each domain.

## Volumes

The compose file creates persistent named volumes:

- `projectk-sql-data` for SQL Server data;
- `projectk-azurite-data` for uploaded blobs.

Back up both before updates or server migration.

## Useful commands

```bash
docker compose ps
docker compose logs -f projectk-api
docker compose logs -f projectk-web
docker compose down
docker compose pull
docker compose up -d
```

## Building images locally

The release bundle uses published container images and does not need .NET, Node.js, npm, or NuGet credentials. If you build images locally from the repository and private GitHub NuGet packages are required, set PROJECTK_NUGET_AUTH_TOKEN in your local environment or .env file.


