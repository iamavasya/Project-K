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

Бекап робиться зсередини контейнера SQL Server, стек при цьому працює. Пароль береться з
`MSSQL_SA_PASSWORD` усередині контейнера: compose уже передав туди `PROJECTK_SQL_PASSWORD` із
`.env`. Тому команда загорнута в `sh -c '…'` — інакше змінну розкрила б оболонка твого компʼютера,
де її немає, і пароль вийшов би порожнім.

```bash
docker compose exec projectk-sql sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [projectK] TO DISK = N'"'"'/var/opt/mssql/data/projectK.bak'"'"' WITH INIT, NAME = '"'"'projectK backup'"'"'"'
```

PowerShell:

```powershell
docker compose exec projectk-sql sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [projectK] TO DISK = N''/var/opt/mssql/data/projectK.bak'' WITH INIT, NAME = ''projectK backup''"'
```

Наприкінці SQL Server пише рядок на кшталт:

```text
BACKUP DATABASE successfully processed 1234 pages in 0.456 seconds (21.142 MB/sec).
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

## Повернути стек після копії

Після `docker compose down` Лілейка недоступна. Копія файлів займає секунди чи хвилини, залежно від
кількості фото. Одразу після неї підніми стек знову:

```bash
docker compose up -d
docker compose ps
```

Усі чотири сервіси мають бути `running`, а API — `healthy` (протягом хвилини після старту).

## Відновлення

Відновлюй у чистий стек тієї самої або новішої версії Лілейки. Поклади поруч із `compose.yml` свій
`.env` (той самий, що був: там адреси й секрети), `projectK.bak` і `projectk-azurite-data.tar.gz`.
API запускай лише в кінці: якщо він стартує раніше, то створить порожню базу, і відновлення доведеться
робити поверх неї.

1. Підняти лише SQL Server і сховище файлів (це створить томи):

   ```bash
   docker compose up -d projectk-sql projectk-azurite
   ```

2. Покласти копію бази в контейнер і відновити її:

   ```bash
   docker cp ./projectK.bak $(docker compose ps -q projectk-sql):/var/opt/mssql/data/projectK.bak
   docker compose exec projectk-sql sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE [projectK] FROM DISK = N'"'"'/var/opt/mssql/data/projectK.bak'"'"' WITH REPLACE"'
   ```

   Очікуваний кінець виводу: `RESTORE DATABASE successfully processed … pages`.

3. Повернути файли (сховище на цей час зупинити):

   ```bash
   docker compose stop projectk-azurite
   docker run --rm -v projectk-azurite-data:/data -v "$PWD:/backup" alpine sh -c "cd /data && tar xzf /backup/projectk-azurite-data.tar.gz"
   ```

4. Запустити все:

   ```bash
   docker compose up -d
   docker compose ps
   ```

   Якщо версія Лілейки новіша за ту, з якої робили копію, API сам застосує міграції бази при старті.

5. Перевірити: відкрий Лілейку, увійди, переконайся, що курені, люди й фото на місці. Версія видно
   внизу бічного меню й на сторінці «Про Лілейку».

PowerShell: у кроках 2 і 3 заміни `$(docker compose ps -q projectk-sql)` на змінну, як у розділі
«Копія бази», а `"$PWD:/backup"` — на `"${PWD}:/backup"`.

## Розклад

Мінімум — перед кожним оновленням. Для живого куреня розумно робити `.bak` щотижня і тримати копії
поза сервером. Приклад для Linux: скрипт `/opt/projectk/backup.sh`

```bash
#!/bin/sh
set -e
cd /opt/projectk
docker compose exec -T projectk-sql sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [projectK] TO DISK = N'"'"'/var/opt/mssql/data/projectK.bak'"'"' WITH INIT"'
docker cp "$(docker compose ps -q projectk-sql)":/var/opt/mssql/data/projectK.bak "backups/projectK-$(date +%F).bak"
find backups -name 'projectK-*.bak' -mtime +56 -delete
```

і рядок у `crontab -e` — щонеділі о 03:00:

```text
0 3 * * 0 /opt/projectk/backup.sh >> /opt/projectk/backups/backup.log 2>&1
```

Теку `backups` синхронізуй кудись поза сервер (rclone, хмарний диск): копія на тому самому диску
не врятує, якщо зламається сам диск.
