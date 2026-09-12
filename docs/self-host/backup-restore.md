# Резервні копії та відновлення

Дані self-host-інсталяції живуть у двох томах Docker:

- `projectk-sql-data` — база SQL Server;
- `projectk-azurite-data` — завантажені файли (фото, сильветки) в Azurite.

Роби копію обох томів перед оновленням, переїздом на інший сервер, зміною compose-файлів і перед
будь-яким відкатом.

Під час звичайного обслуговування **не виконуй `docker compose down -v`**: прапорець `-v` видаляє
томи з даними.

## Перевірити томи

```bash
docker volume ls --filter name=projectk
```

У справній інсталяції є обидва:

```text
projectk-sql-data
projectk-azurite-data
```

## Копія бази SQL Server

Бекап робиться зсередини контейнера SQL Server (пароль — той, що в `.env`):

```bash
docker compose exec projectk-sql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P "$PROJECTK_SQL_PASSWORD" \
  -C \
  -Q "BACKUP DATABASE [projectK] TO DISK = N'/var/opt/mssql/data/projectK.bak' WITH NOFORMAT, INIT, NAME = 'projectK backup'"
```

Забрати файл із контейнера:

```bash
docker cp $(docker compose ps -q projectk-sql):/var/opt/mssql/data/projectK.bak ./projectK.bak
```

PowerShell:

```powershell
$containerId = docker compose ps -q projectk-sql
docker cp "$containerId`:/var/opt/mssql/data/projectK.bak" ./projectK.bak
```

## Копія файлів (Azurite)

Перед копією тому зупини стек:

```bash
docker compose down
```

Заархівувати том:

```bash
docker run --rm \
  -v projectk-azurite-data:/data \
  -v "$PWD:/backup" \
  alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

PowerShell:

```powershell
docker run --rm `
  -v projectk-azurite-data:/data `
  -v "${PWD}:/backup" `
  alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

## Архів обох томів (запасний варіант)

Лише при зупиненому SQL Server. Для бази краще `.bak`, але архів томів зручний для переїзду на
інший сервер:

```bash
docker compose down
docker run --rm -v projectk-sql-data:/data -v "$PWD:/backup" alpine tar czf /backup/projectk-sql-data.tar.gz -C /data .
docker run --rm -v projectk-azurite-data:/data -v "$PWD:/backup" alpine tar czf /backup/projectk-azurite-data.tar.gz -C /data .
```

## Відновлення

Відновлюй у чистий стек тієї самої або новішої версії Лілейки. Порядок: спершу SQL Server, потім
дані Azurite, потім запуск API і фронтенду. Після старту API сам застосує міграції, якщо версія
новіша за ту, з якої робили копію.

Зберігай `.env` разом із копіями: там публічні адреси й секрети, без яких інсталяція не
підніметься.

## Розклад

Мінімум — перед кожним оновленням. Для живого куреня розумно робити `.bak` щотижня скриптом у cron
і тримати копії поза сервером.
