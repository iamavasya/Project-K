# Backup and restore

Project-K self-host data lives in two Docker volumes:

- `projectk-sql-data`: database data;
- `projectk-azurite-data`: uploaded files stored by Azurite.

Back up both volumes before updating, moving servers, or changing compose files.

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

## Restore notes

Restore into a clean stack with matching or newer Project-K version. Restore SQL Server first, then Azurite data, then start the API and frontend.

Always keep the `.env` file together with backups. It contains the public URLs and secrets required by the installation.
