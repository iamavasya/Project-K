# Self-host Лілейки

Лілейку можна підняти двома шляхами:

- локально з клонованого репозиторію — для проби і розробки;
- Docker self-host з готових образів GHCR через bundle релізу GitHub, без git і без збірки.

В обох випадках фронтенд (Angular) і бекенд (ASP.NET Core API) — окремі процеси; файли зберігає
Azurite, сумісне з Azure Blob сховище.

## Локально з клонованого репозиторію

Потрібні git, .NET SDK, Node.js, npm, SQL Server і Azurite на машині.

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

Адреси:

- фронтенд: http://localhost:4200
- API: http://localhost:5205
- Swagger: http://localhost:5205/swagger
- Azurite: http://127.0.0.1:10000/devstoreaccount1

Зупинити: `./scripts/stop-local.ps1` або `./scripts/stop-local.sh`. Логи — в `.tmp/local-run/logs`.

Альтернатива без встановлення SDK — контейнерний стек із `docker/`: `./dev.sh up dev --build`
(див. `docker/README.md`).

## Docker self-host без git

Для сервера чи машини, де є лише Docker.

Образи публікуються в GitHub Container Registry:

- `ghcr.io/iamavasya/projectk-api:<версія>`
- `ghcr.io/iamavasya/projectk-web:<версія>`

Найпростіший шлях — bundle з GitHub Releases: `projectk-<версія>-docker-selfhost.zip` або `.tar.gz`.
Всередині `docker-compose.yml`, `.env.example` і ця документація. Бінарників там немає і нічого не
збирається — Docker тягне готові образи.

Windows:

```powershell
Expand-Archive projectk-<версія>-docker-selfhost.zip
cd projectk-<версія>-docker-selfhost
copy .env.example .env
notepad .env
docker compose up -d
```

Linux/macOS:

```bash
tar -xzf projectk-<версія>-docker-selfhost.tar.gz
cd projectk-<версія>-docker-selfhost
cp .env.example .env
nano .env
docker compose up -d
```

Два значення треба задати до першого запуску — з прикладними API не стартує:

- `PROJECTK_JWT_KEY` — щонайменше 32 випадкові символи (`openssl rand -base64 48`);
- `PROJECTK_SQL_PASSWORD` — і той самий пароль усередині `PROJECTK_DB_CONNECTION_STRING`.

Відкрий http://localhost:8080. Перший візит веде на майстер налаштування, який створює
адміністратора. До цього жодного акаунта немає і нічого не засіюється.

Bundle публікує один порт — веб. API, SQL Server і сховище лишаються в мережі compose: nginx у
`projectk-web` проксить `/api` до API і `/blob` до сховища, тому `PROJECTK_API_URL` — відносний
`/api`, і ззовні більше нічого не має бути видно.

Перевірка здоровʼя API (сам API назовні не опублікований):

```bash
docker compose exec projectk-api curl -fsS http://localhost:8080/health
```

## Перед виходом в інтернет

Bundle слухає звичайний HTTP і розрахований на реверс-проксі, який термінує TLS (Caddy, nginx,
Traefik або проксі хостера). Далі:

1. У `.env` постав публічну адресу: `PROJECTK_PUBLIC_URL`, `PROJECTK_CORS_ORIGIN` і хост у
   `PROJECTK_BLOB_PUBLIC_BASE_URL` стають `https://твій.домен`. `PROJECTK_API_URL` лишається `/api`.
2. Спрямуй реверс-проксі на `127.0.0.1:8080` (або що в `PROJECTK_WEB_PORT`) і передавай адресу
   відвідувача; nginx у `projectk-web` віддає її API як `X-Real-IP` — за нею обмежуються спроби входу.
3. SQL Server і Azurite не публікуй. Якщо база потрібна з хоста для бекапу — додай
   `docker-compose.override.yml`, який публікує `projectk-sql` лише на `127.0.0.1:1433`.
4. Фото — публічні блоби за задумом: хто має URL, той відкриє без входу. URL невгадувані, але не
   захищені.
5. Зроби копію обох томів до перших реальних даних і перед кожним оновленням —
   [backup-restore.md](backup-restore.md).
6. Увімкни двофакторну автентифікацію адміністратору і кожному Звʼязковому. У налаштуваннях
   системи її можна зробити обовʼязковою для привілейованих акаунтів (на self-host вона типово
   вимкнена, `Security__EnforcePrivilegedMFA`).

## Пошта

Без поштового провайдера запрошення й відновлення пароля нікуди не підуть — у логах API буде
лише текст листа (`Email__Provider=Mock`). Для живої інсталяції задай Resend:
`Email__Provider=Resend`, `Email__ApiKey`, `Email__FromEmail` на своєму домені.

## Установка прямо з образів GHCR

Bundle — підтримуваний шлях: compose-файл у ньому узгоджений з релізом. Якщо в тебе вже є своя
тека розгортання — створи `.env` за `docker/selfhost/.env.example`, скопіюй
`docker/selfhost/compose.bundle.yml` як `docker-compose.yml` і виконай:

```bash
docker compose pull
docker compose up -d
```

Перевірити доступність образів:

```bash
docker pull ghcr.io/iamavasya/projectk-api:<версія>
docker pull ghcr.io/iamavasya/projectk-web:<версія>
```

Тег `beta` вказує на останню бету. Для живих інсталяцій бери явну версію; `beta` — лише якщо
свідомо йдеш за найновішим.

## Контейнери

Compose-файл bundle запускає лише готові образи:

- `projectk-web` — nginx зі збіркою Angular;
- `projectk-api` — ASP.NET Core API;
- `projectk-sql` — SQL Server;
- `projectk-azurite` — емулятор сховища Azurite.

Фронтенд читає `PROJECTK_API_URL`, `PROJECTK_ENVIRONMENT_NAME` і `PROJECTK_APP_NAME` при старті
контейнера і записує в `env.js`, тож один образ працює на будь-якому домені без перезбірки.

## Томи

Compose створює іменовані томи зі стабільними назвами:

- `projectk-sql-data` — дані SQL Server;
- `projectk-azurite-data` — завантажені файли.

Контейнери можна перестворювати при оновленнях — дані лишаються в томах. Не виконуй
`docker compose down -v`, якщо не хочеш видалити всі дані. Копія обох томів — перед оновленням і
переїздом.

## Корисні команди

```bash
docker compose ps
docker compose logs -f projectk-api
docker compose logs -f projectk-web
docker compose down
docker compose pull
docker compose up -d
```

## Збірка образів локально

Bundle не потребує .NET, Node.js, npm чи NuGet-токена. Якщо збираєш образи з репозиторію сам —
для приватних NuGet-пакетів потрібен `NUGET_AUTH_TOKEN` (або `PROJECTK_NUGET_AUTH_TOKEN`) у
середовищі; в `.env` його не клади.

Далі: [оновлення](update.md), [резервні копії](backup-restore.md).
