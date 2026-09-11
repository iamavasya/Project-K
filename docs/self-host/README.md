# Project-K self-host

Project-K supports two self-host friendly startup paths:

- local try-out from a cloned repository;
- Docker self-host from published GHCR images through a GitHub Release bundle without git.

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

Project-K self-host images are published to GitHub Container Registry:

- `ghcr.io/iamavasya/projectk-api:<version>`
- `ghcr.io/iamavasya/projectk-web:<version>`

The easiest install path is the GitHub Release bundle. It contains `docker-compose.yml`, `.env.example`, and the self-host docs. It does not contain app binaries and does not build anything locally; Docker pulls the published images from GHCR.

Download the `projectk-<version>-docker-selfhost.zip` or `.tar.gz` bundle from GitHub Releases.

Windows:

```powershell
Expand-Archive projectk-0.14.2-beta-docker-selfhost.zip
cd projectk-0.14.2-beta-docker-selfhost
copy .env.example .env
notepad .env
docker compose up -d
```

Linux/macOS:

```bash
tar -xzf projectk-0.14.2-beta-docker-selfhost.tar.gz
cd projectk-0.14.2-beta-docker-selfhost
cp .env.example .env
nano .env
docker compose up -d
```

Two values must be set before the first start, and the API refuses to come up while they are the
example ones:

- `PROJECTK_JWT_KEY` — at least 32 random characters (`openssl rand -base64 48`);
- `PROJECTK_SQL_PASSWORD` — and the same password inside `PROJECTK_DB_CONNECTION_STRING`.

Open http://localhost:8080. The first visit lands on the setup wizard, which creates the
administrator account; no account exists before that and none is seeded.

The bundle publishes one port, the web one. The API, SQL Server and the blob store stay on the
compose network: nginx inside `projectk-web` forwards `/api` to the API and `/blob` to the store,
so `PROJECTK_API_URL` is the relative `/api` and nothing else has to be reachable from outside.

Default Docker URLs:

- App: http://localhost:8080
- API health (the API itself is not published): `docker compose exec projectk-api curl -fsS http://localhost:8080/health`

## Before exposing to the internet

The bundle listens on plain HTTP and is meant to sit behind a reverse proxy that terminates TLS
(Caddy, nginx, Traefik, or the hosting provider's). Then:

1. Put the public address in `.env` — `PROJECTK_PUBLIC_URL`, `PROJECTK_CORS_ORIGIN` and the host
   part of `PROJECTK_BLOB_PUBLIC_BASE_URL` all become `https://your.domain`. `PROJECTK_API_URL`
   stays `/api`.
2. Point the reverse proxy at `127.0.0.1:8080` (or whatever `PROJECTK_WEB_PORT` says) and let it
   pass the visitor's address along; nginx in `projectk-web` forwards it to the API as `X-Real-IP`,
   which is what sign-in attempts are rate-limited by.
3. Keep SQL Server and Azurite unpublished. If you need the database from the host for a backup,
   add a `docker-compose.override.yml` that publishes `projectk-sql` on `127.0.0.1:1433` only.
4. Photos are public blobs by design: anyone with a photo's URL can open it without signing in.
   The URLs are unguessable, but they are not protected.
5. Back up both volumes before the first real data goes in and before every update —
   `backup-restore.md`.
6. Turn on two-factor authentication for the administrator and for every Зв'язковий account.
   The system settings let you require it for privileged accounts.

## Install directly from GHCR images

Use the release bundle when possible. It is the supported git-free installation path and keeps the compose file aligned with the release.

If you already have your own deployment folder, create `.env` from `docker/selfhost/.env.example`, copy `docker/selfhost/compose.bundle.yml` as `docker-compose.yml`, and run:

```bash
docker compose pull
docker compose up -d
```

For a quick image availability check:

```bash
docker pull ghcr.io/iamavasya/projectk-api:0.14.2-beta
docker pull ghcr.io/iamavasya/projectk-web:0.14.2-beta
```

The `beta` tag is also published for the latest beta build:

```bash
docker pull ghcr.io/iamavasya/projectk-api:beta
docker pull ghcr.io/iamavasya/projectk-web:beta
```

Prefer version tags such as `0.14.2-beta` for real self-host installations. Use `beta` only when you intentionally want to follow the latest beta image.

## Containers

The release bundle compose file uses published GHCR container images only and does not build from source. It starts:

- `projectk-web`: nginx serving the Angular build;
- `projectk-api`: ASP.NET Core API;
- `projectk-sql`: SQL Server;
- `projectk-azurite`: Azurite storage emulator.

The frontend image reads `PROJECTK_API_URL` at container startup and writes it into `env.js`, so you can reuse the same image across hosts without rebuilding it for each domain.

## Volumes

The compose file creates persistent named volumes with stable Docker names:

- `projectk-sql-data` for SQL Server data;
- `projectk-azurite-data` for uploaded blobs.

Containers can be recreated during updates. These volumes keep the data. Do not run `docker compose down -v` unless you intentionally want to delete all Project-K self-host data. Back up both volumes before updates or server migration.

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

The release bundle uses published GHCR container images and does not need .NET, Node.js, npm, or NuGet credentials. If you build images locally from the repository and private GitHub NuGet packages are required, set PROJECTK_NUGET_AUTH_TOKEN in your local environment or .env file.
