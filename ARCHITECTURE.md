# Архітектура ProjectK

Мапа системи: з чого вона складається, куди йде запит і де що шукати. Правила («як писати новий
код») — у [CONTRIBUTING.md](CONTRIBUTING.md); цей файл описує, **що вже є**.

---

## Що це за система

Застосунок для управління пластовим куренем: членство, проводи (уряди), календар і планування,
проби та вмілості, публічні оголошення, онбординг нових людей.

Ключова відмінність від типової рольової системи: **доступ визначається урядом, який людина
обіймає**, а не роллю акаунта. Акаунт знає лише, адміністратор це чи ні. Усе решта — «чи може ця
людина редагувати цього члена» — виводиться з уряду. Тому `LeadershipController` змінює не довідник,
а права.

---

## Складові

```
Frontend/projectk-frontend     Angular 22, standalone, signals, optimus-ui 2 + Tailwind
Backend/ProjectK.Backend       .NET 10, ASP.NET Core, EF Core 10, MediatR
docker/                        одна збірка образу на всі середовища
scripts/                       dev.sh / dev.ps1 — підняти будь-яке середовище локально
```

Три залежності живуть як зовнішні пакети, не як проєкти в солюшені: `ProjectK.Optimization`
(кеш і профілювання), `ProjectK.ProbeAndBadges.DependencyInjection` (каталоги проб і вмілостей),
`ProjectK.ProbeAndBadges.Abstractions`. Каталоги **читаються, а не редагуються** через застосунок —
тому `BadgesCatalogController` і `ProbesCatalogController` не мають операцій запису.

---

## Бекенд: чотири проєкти

```
ProjectK.Common          → (нічого)
ProjectK.Infrastructure  → Common
ProjectK.BusinessLogic   → Common
ProjectK.API             → Common, BusinessLogic, Infrastructure
```

`Infrastructure` і `BusinessLogic` не знають одне про одного. Коли бізнес-логіці потрібен
зовнішній світ, інтерфейс оголошується в `Common`, а реалізується в `Infrastructure`:
`IKurinReportSource`, `IPublicAnnouncementImageStore`, `IAppUserRepository`, `IUnitOfWork`.

Розподіл відповідальності — у таблиці в [CONTRIBUTING.md](CONTRIBUTING.md#шари).

### Модулі

Однакові по обидва боки межі — і в `BusinessLogic/Modules/`, і в `API/Controllers/`:

| Модуль | Про що |
|---|---|
| `AuthModule` | вхід, MFA, онбординг, скидання пароля, налаштування інстансу, первинна настройка |
| `UsersModule` | акаунти: безпека, профіль акаунта, збережені розкладки дашборда |
| `KurinModule` | курінь, гуртки, члени, уряди, календар, планування, відзнаки, перестороги |
| `ProbesAndBadgesModule` | каталоги проб і вмілостей та поступ конкретного члена |
| `InfrastructureModule` | сповіщення, публічні оголошення |

**Член ≠ користувач.** Член — це людина в курені; користувач — спосіб увійти. Одне може існувати
без іншого: члена заводять до того, як він отримає акаунт, і не кожен член його отримує.

---

## Шлях запиту

```
HTTP
  ↓  Serilog request logging, forwarded headers, rate limiting
  ↓  автентифікація JWT (access у заголовку, refresh у httpOnly-cookie)
  ↓     сесія = рядок у UserRefreshTokens; акаунт може бути в кількох місцях одночасно
  ↓  політика авторизації   AuthorizationPolicies.*   — «якого рівня має бути викликач»
  ↓  ResourceAuthorize      IResourceAccessService    — «чи саме цей об'єкт йому доступний»
  ↓  контролер: жодних рішень, лише _mediator.Send(...)
  ↓  MediatR pipeline: валідація (FluentValidation) → хендлер
  ↓  хендлер: доменне рішення; дані бере через репозиторії з Common-інтерфейсів
  ↓  IUnitOfWork.SaveChangesAsync
  ↓  ServiceResult<T>
  ↓  ToActionResult(this) → HTTP
```

Дві сходинки авторизації розділені навмисно. Політика відповідає на питання «чи має ця людина
взагалі право на такі дії» (`RequireGroupLeadership`), а `ResourceAuthorize` — «чи цей конкретний
член / гурток / курінь у її сфері» (`AccessScope`: `Own`, `OwnGroups`, `KurinWide`).

Політика лишається лише там, де вона **вужча** за ресурсну перевірку — тобто справді щось вирішує.
Там, де вона просто повторювала мапу дозволів, її знято: дві копії правил розходяться, і вигравала
суворіша, даючи 403, якого ніщо не пояснює. `PolicyMatchesPermissionMapTests` валиться, якщо читання
запису колись стане суворішим за його зміну.

**`ServiceResult<T>` — єдина форма відповіді.** Помилка завжди `{ error, message }`; перетворення
в HTTP живе в одному місці — `ServiceResultExtensions.ToActionResult`.

---

## Авторизація

```
LeadershipRole + LeadershipType      уряд, який людина обіймає
        ↓ SystemRole.ForOffice()
SystemRole                           роль доступу, дзеркало уряду
        ↓ RolePermissionMap
Permission                           напр. Group:Manage:KurinWide
```

Уряди — джерело; ролі доступу синхронізуються з них автоматично. Другого списку «хто керує
куренем» немає: усе виводиться з `RolePermissionMap`.

Політики оголошені **один раз** — `AuthorizationPolicies.AddProjectPolicies()`; і хост, і тестові
хости беруть їх звідти. Матриця `AuthorizationBaselineMatrixTests` тримає очікувану політику для
кожного ендпоінта: новий ендпоінт без запису в матриці валить тест.

---

## Дані та зовнішні системи

| Що | Чим | Де код |
|---|---|---|
| База | SQL Server, EF Core 10, міграції в збірці | `Infrastructure/DbContexts`, `Infrastructure/Migrations` |
| Сесії | рядок на кожен вхід (`IRefreshTokenStore`); вихід обриває одну, зміна пароля — усі | `Infrastructure/Repositories/AuthModule` |
| Доступ до даних | репозиторії поверх `BaseEntityRepository<T>`, транзакції через `IUnitOfWork` | `Infrastructure/Repositories` |
| Файли | Azure Blob Storage (локально — Azurite) | `Infrastructure/Services/BlobStorageService` |
| Пошта | запрошення, скидання пароля, сповіщення | `Infrastructure/Services` |
| PDF | QuestPDF; звіт куреня збирається з `IKurinReportSource` | `Infrastructure`, `BusinessLogic` |
| Логи | Serilog: файл, Application Insights, Telegram-сінк для дев-алертів | `API/Program.cs`, `Infrastructure` |

Фонові служби: прибирання аудиту (`AuditCleanupBackgroundService`), закінчення строку пересторог
(`MemberWarningExpiryBackgroundService`), прибирання осиротілих фото
(`OrphanPhotoCleanupService`).

---

## Фронтенд

```
src/app/features/
  authModule/           вхід, онбординг, скидання пароля, guard-и
  adminModule/          публічні оголошення, адміністрування
  kurinModule/          курінь, гуртки, члени, календар, проби та вмілості
  notificationsModule/  інбокс
  systemModule/         налаштування, службові екрани
```

Angular 22, standalone-компоненти, signals (декораторів не лишилось). Доступ до маршрутів —
`capabilityGuard` з `RouteCapability` (`admin | kurinManagement | groupLeadership`), тобто ті самі
поняття, що й на бекенді, а не назви ролей.

Візуальна система описана в [BRANDBOOK.md](BRANDBOOK.md); іменування файлів і класів —
у [CONTRIBUTING.md](CONTRIBUTING.md#фронтенд).

---

## Середовища

Один образ, середовище обирається в рантаймі через `ASPNETCORE_ENVIRONMENT`:

| Середовище | Для чого |
|---|---|
| `Development` | локальна розробка; Swagger увімкнено |
| `E2E` | стек під Playwright; додається `E2ETestController` з фікстурами |
| `SelfHost` | самостійне розгортання; доступний майстер первинної настройки |
| `Staging` | перевірка релізу; Swagger увімкнено |
| `Tailscale` | закритий доступ через tailnet |
| `Production` | прод; Swagger вимкнено, `LoadTestLoginKey` порожній |

Підняти будь-яке: `./scripts/dev.sh up <env>`. Деталі — у [docker/README.md](docker/README.md).

---

## Опис API

Swagger віддає повний опис поверхні в `Development` і `Staging`: `/swagger`. Кожен ендпоінт має
`<summary>` (навіщо він) і, де це неочевидно, `<remarks>` (хто може викликати, що ще станеться).
Форма помилки описана один раз — `UnifiedErrorResponsesFilter`, а не повторюється на кожній дії.

Перевірити, що документ узагалі збирається, можна без запуску застосунку:

```bash
swagger tofile --output swagger.json Backend/ProjectK.Backend/ProjectK.API/bin/Release/net10.0/ProjectK.API.dll v1
```

---

## Тести

| Проєкт | Що покриває |
|---|---|
| `ProjectK.BusinessLogic.Tests` | хендлери: доменні рішення |
| `ProjectK.Infrastructure.Tests` | репозиторії, сховища |
| `ProjectK.API.Tests` | контролери, серіалізація, **і вся авторизація** — матриця політик, ресурсні перевірки, анонімний доступ |
| `ProjectK.LoadTests` | навантаження |
| `Frontend/.../e2e` | Playwright проти docker-стека |

Бейслайн і команди перевірки — у [CONTRIBUTING.md](CONTRIBUTING.md#перевірка).
