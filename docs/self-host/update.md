# Updating a self-host installation

This guide assumes you installed Project-K using the Docker self-host release bundle.

## Before updating

1. Read the release notes.
2. Back up SQL Server and Azurite data.
3. Save your current `.env` file.

## Update with the same bundle folder

Edit `.env` and set the target version:

```text
PROJECTK_VERSION=0.14.1-beta
```

Pull the new images and restart:

```bash
docker compose pull
docker compose up -d
```

The API applies EF Core migrations on startup.

## Update from a new release bundle

1. Download the new `projectk-<version>-docker-selfhost` bundle.
2. Copy your existing `.env` into the new folder.
3. Review `.env.example` for new variables.
4. Run:

```bash
docker compose pull
docker compose up -d
```

## Rollback

Rollback is only safe if the database schema is compatible with the previous version. Prefer restoring from backup for major changes or failed migrations.
