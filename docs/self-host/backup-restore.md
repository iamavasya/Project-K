# Backup and restore

Project-K self-host data lives in two Docker volumes:

- `projectk-sql-data`: SQL Server database data;
- `projectk-azurite-data`: uploaded files stored by Azurite.

Back up both volumes before updating, moving servers, changing compose files, or testing a rollback.

Do not use `docker compose down -v` during normal maintenance. The `-v` flag deletes the volumes that hold Project-K data.

## Quick volume check

```bash
docker volume ls --filter name=projectk
```

A healthy `0.14.2-beta` self-host installation should have these volumes:

```text
projectk-sql-data
projectk-azurite-data
```

## SQL Server backup

Create a database backup from inside the SQL Server container:

```bash
docker compose exec projectk-sql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P "$PROJECTK_SQL_PASSWORD" \
  -C \
  -Q "BACKUP DATABASE [projectK] TO DISK = N'/var/opt/mssql/data/projectK.bak' WITH NOFORMAT, INIT, NAME = 'projectK backup'"
```

Copy the backup file out:

```bash
docker cp $(docker compose ps -q projectk-sql):/var/opt/mssql/data/projectK.bak ./projectK.bak
```

PowerShell equivalent for copying the SQL backup:

```powershell
$containerId = docker compose ps -q projectk-sql
docker cp "$containerId`:/var/opt/mssql/data/projectK.bak" ./projectK.bak
```

## Azurite backup

Stop the stack before making a volume-level backup:

```bash
docker compose down
```

Create an archive of the Azurite volume:

```bash
docker run --rm \
  -v projectk-azurite-data:/data \
  -v "$PWD:/backup" \
  alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

PowerShell equivalent:

```powershell
docker run --rm `
  -v projectk-azurite-data:/data `
  -v "${PWD}:/backup" `
  alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

## Full volume archive fallback

Use this only when SQL Server is stopped. A SQL `.bak` file is preferred for database backups, but a stopped-volume archive is useful for server migration.

```bash
docker compose down
docker run --rm -v projectk-sql-data:/data -v "$PWD:/backup" alpine tar czf /backup/projectk-sql-data.tar.gz -C /data .
docker run --rm -v projectk-azurite-data:/data -v "$PWD:/backup" alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

## Restore notes

Restore into a clean stack with matching or newer Project-K version. Restore SQL Server first, then Azurite data, then start the API and frontend.

Always keep the `.env` file together with backups. It contains the public URLs and secrets required by the installation.
