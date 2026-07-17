# Updating a self-host installation

This guide assumes you installed Project-K using the Docker self-host release bundle.

## Before updating

1. Read the release notes.
2. Back up SQL Server and Azurite data.
3. Save your current `.env` file.
4. Check that your Docker volumes exist:

```bash
docker volume ls --filter name=projectk
```

Project-K self-host keeps user data in Docker named volumes. Updating container images does not delete those volumes.

Never run this command unless you intentionally want to delete all Project-K self-host data:

```bash
docker compose down -v
```

The `-v` flag removes volumes, including SQL Server data and Azurite blob data.

## Update with the same bundle folder

Edit `.env` and set the target version:

```text
PROJECTK_VERSION=0.14.2-beta
```

Pull the new images and restart:

```bash
docker compose pull
docker compose up -d
```

The API applies EF Core migrations on startup.

## Update from a new release bundle

1. Download the new `projectk-<version>-docker-selfhost` bundle.
2. Extract it into a new folder.
3. Copy your existing `.env` into the new folder.
4. Review `.env.example` for new variables.
5. Run:

```bash
docker compose pull
docker compose up -d
```

Starting with `0.14.2-beta`, the bundle uses explicit Docker volume names:

- `projectk-sql-data`
- `projectk-azurite-data`

That keeps data attached to the installation even when the bundle folder name changes.

## Upgrading from 0.14.1-beta bundle volumes

The `0.14.1-beta` bundle used Compose-managed volume names that may include the folder name as a prefix, for example:

```text
projectk-0.14.1-beta-docker-selfhost_projectk-sql-data
projectk-0.14.1-beta-docker-selfhost_projectk-azurite-data
```

Before starting `0.14.2-beta` from a new folder, check your actual volume names:

```bash
docker volume ls --filter name=projectk
```

If your data is in prefixed `0.14.1-beta` volumes, create a backup first, then restore or copy that data into the new stable volumes named `projectk-sql-data` and `projectk-azurite-data`. The safest route is to keep the old bundle folder, create SQL and Azurite backups, then restore them into the `0.14.2-beta` stack.

## Rollback

Rollback is only safe if the database schema is compatible with the previous version. Prefer restoring from backup for major changes or failed migrations.
