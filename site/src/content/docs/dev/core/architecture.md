---
title: 'Архітектура ProjectK'
description: 'Шари, модулі, шлях запиту, авторизація, середовища.'
sidebar:
  order: 1
---

:::note[Джерело]
Ця сторінка збирається з [`ARCHITECTURE.md`](https://github.com/iamavasya/Project-K/blob/main/ARCHITECTURE.md) у репозиторії. Правити треба там.
:::
Мапа системи: з чого вона складається, куди йде запит і де що шукати. Правила («як писати новий
код») — у [CONTRIBUTING.md](/dev/core/contributing/); цей файл описує, **що вже є**.

---

## Що це за система

Застосунок для управління пластовим куренем: членство, проводи (уряди), реєстр складу, календар і
планування, проби та вмілості, онбординг нових людей.

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
`IKurinReportSource`, `ISpreadsheetWriter`, `ISpreadsheetReader`, `IAppUserRepository`, `IUnitOfWork`.

Розподіл відповідальності — у таблиці в [CONTRIBUTING.md](/dev/core/contributing/#шари).

### Модулі

Однакові по обидва боки межі — і в `BusinessLogic/Modules/`, і в `API/Controllers/`:

| Модуль | Про що |
|---|---|
| `AuthModule` | вхід, MFA, онбординг, скидання пароля, налаштування інстансу, первинна настройка |
| `UsersModule` | акаунти: безпека, профіль акаунта, збережені розкладки дашборда |
| `KurinModule` | курінь, гуртки, члени, уряди, календар, планування, відзнаки, перестороги |
| `ProbesAndBadgesModule` | каталоги проб і вмілостей та поступ конкретного члена |
| `InfrastructureModule` | сповіщення |

**Член ≠ користувач.** Член — це людина; користувач — спосіб увійти. Одне може існувати без
іншого: людину заводять до того, як вона отримає акаунт, і не кожна його отримує.

**Людина ≠ курінь.** `Member` — це людина сама по собі: імʼя, дата народження, проби, вмілості,
рівні, відзнаки, перестороги. Де вона належить, каже окремий запис — `Membership` (курінь, гурток,
вид членства, коли прийшла й коли пішла). Тому людина може бути в кількох куренях одночасно, а вихід
із куреня чи його розпуск не стирає нічого з того, що вона здобула: історія належить їй, а курінь на
цих записах — лише штамп «де це сталося».

### Межі модулів

Модулі не читають таблиць одне одного. Усе, що один модуль питає в іншого, проходить через контракт
у `Common/Interfaces/Modules/` — рівно ті питання, які колись можна буде поставити по мережі:

| Контракт | Хто відповідає | На що |
|---|---|---|
| `IMemberDirectory` | мембер | хто ця людина; пошук за публічним кодом |
| `IMembershipDirectory` | курінь | де людина належить і належала |
| `IOfficeDirectory` | курінь | які уряди акаунт обіймає в цьому курені |
| `IMemberProgressDirectory` | проби й вмілості | як далеко людина зайшла |

Межі тримає не домовленість, а `ProjectK.Architecture.Tests`: правила читають IL, тож звернення до
чужого репозиторію чи чужої сутності валить тест навіть тоді, коли воно сховане всередині методу й
ніде не видно в сигнатурах. Там, де борг ще є, він занесений у baseline правила — тобто
перелічений поіменно, а не непомічений.

Мембера будували так, щоб його можна було винести в окремий сервіс: контракт замість репозиторію,
подія замість виклику, ключ замість навігації. Виносити його в 0.20.0 ніхто не збирався — але
кожне з цих правил уже перевіряється.

---

## Шлях запиту

```
HTTP
  ↓  Serilog request logging, forwarded headers, rate limiting
  ↓  автентифікація JWT (access у заголовку, refresh у httpOnly-cookie)
  ↓     сесія = рядок у UserRefreshTokens; акаунт може бути в кількох місцях одночасно
  ↓     другий фактор: пароль → короткий mfaToken (5 хв, окрема audience) → код + mfaToken;
  ↓     код без mfaToken відхиляється — другий фактор ніколи не буває єдиним
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
активний курінь у токені            у якому курені людина зараз діє
        ↓ IOfficeDirectory           уряди цього акаунта саме в цьому курені
LeadershipRole + LeadershipType      уряд, який людина обіймає
        ↓ SystemRole.ForOffice()
SystemRole                           роль доступу, дзеркало уряду
        ↓ RolePermissionMap
Permission                           напр. Group:Manage:KurinWide
```

Уряди — джерело; ролі доступу **виводяться з них на кожен вхід**, а не зберігаються на акаунті.
Другого списку «хто керує куренем» немає: усе виводиться з `RolePermissionMap`.

Ключове тут — слово «активний». Ролі рахуються для одного куреня за раз (`IAccessContextResolver`),
бо та сама людина може бути виховником в одному курені й звичайним членом у другому. Доки ролі
писалися в Identity як глобальні, друге членство протягло б права виховника й туди — тому
`LeadershipRoleSyncService` знято, а в сховищі акаунтів лишились тільки `Admin` і `Member`.
Перемикання куреня (`POST api/auth/kurin-scope`) — це не фільтр списку, а перевидача токена; воно
відкрите будь-кому, хто має **активне** членство в тому курені, і лише адміністратор може стояти
поза всіма.

Авторизація навмисно не знає про мембера: усе, що їй потрібно, вона читає через членства й уряди за
ключем акаунта. Це не побажання, а правило `Authorization_ShouldNotKnowAboutMember` в арх-тестах.

Політики оголошені **один раз** — `AuthorizationPolicies.AddProjectPolicies()`; і хост, і тестові
хости беруть їх звідти. Матриця `AuthorizationBaselineMatrixTests` тримає очікувану політику для
кожного ендпоінта: новий ендпоінт без запису в матриці валить тест.

---

## Дані та зовнішні системи

| Що | Чим | Де код |
|---|---|---|
| База | SQL Server, EF Core 10, міграції в збірці | `Infrastructure/DbContexts`, `Infrastructure/Migrations` |
| Сесії | рядок на кожен вхід (`IRefreshTokenStore`); вихід обриває одну, зміна пароля й призупинення акаунта адміністратором — усі; призупинений (`OnboardingStatus.Suspended`) не проходить ні вхід, ні другий фактор, ні оновлення токена | `Infrastructure/Repositories/AuthModule` |
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
  adminModule/          адміністрування, користувачі, заявки
  kurinModule/          курінь, гуртки, члени, календар, проби та вмілості
  notificationsModule/  інбокс
  systemModule/         налаштування, службові екрани
```

Angular 22, standalone-компоненти, signals (декораторів не лишилось). Доступ до маршрутів —
`capabilityGuard` з `RouteCapability` (`admin | kurinManagement | groupLeadership`), тобто ті самі
поняття, що й на бекенді, а не назви ролей.

Візуальна система описана в [BRANDBOOK.md](/dev/core/brandbook/); іменування файлів і класів —
у [CONTRIBUTING.md](/dev/core/contributing/#фронтенд).

---

## Середовища

Один образ, середовище обирається в рантаймі через `ASPNETCORE_ENVIRONMENT`:

| Середовище | Для чого |
|---|---|
| `Development` | локальна розробка; Swagger увімкнено; сідер заводить демо-курінь і `admin@projectk.com` |
| `E2E` | стек під Playwright; лише тут у застосунку існує `E2ETestController` з фікстурами — в інших середовищах його прибирає з моделі `E2EOnlyControllerFeatureProvider` |
| `SelfHost` | самостійне розгортання; сідер не заводить нікого |
| `Staging` | перевірка релізу; Swagger увімкнено; демо-даних немає, дані переживають перезапуск |
| `Tailscale` | закритий доступ через tailnet — стенд для людей, не демо: сідер його не чіпає, дані переживають перезапуск |
| `Production` | прод; Swagger вимкнено, `LoadTestLoginKey` порожній; **демо-акаунтів немає** |

Майстер первинної настройки (`api/auth/setup`) відкритий у **кожному** середовищі, доки в базі
немає жодного адміністратора, і зачиняється, щойно він зʼявився. Тому середовища з сідером його
ніколи не бачать, а свіжий `Production` чи `SelfHost` отримує першого адміністратора саме тут — а
не з пароля, записаного в репозиторії, як було до 1.0.

Підняти будь-яке: `./scripts/dev.sh up <env>`. Деталі — у [docker/README.md](docker/README.md).

Демо-курінь і демо-акаунти сідер заводить (і стирає при кожному старті) **лише** в `Development` і
`E2E`. Раніше це стосувалося й `Staging` та `Tailscale`, і стенд, який показували людям, губив усе
введене при наступному запуску.

**Дев-інструменти.** На `Development`, `E2E` і `Tailscale` в застосунку є `DevToolsController`
(`api/dev/impersonate`, `api/dev/impersonate/member`, `api/dev/return`) і перемикач ролей на правому
краю екрана: адміністратор заходить як той, хто тримає обраний уряд у курені на екрані (Звʼязковий,
впорядник, курінний, скарбник), як юнак без уряду або як та людина, чия картка відкрита, і
повертається за 12-годинним квитком (`IJwtService`, окрема audience). Перемикання з однієї позиченої
ролі в іншу йде через повернення: фронт спершу віддає квиток (`api/dev/return`), потім позичає знову,
бо позичена сесія не адмін і сама позичати не може. На решті тирів `DevOnlyControllerFeatureProvider` знімає контролер з моделі, а фронт не
рендерить перемикач у production-збірці.

**Довірений пристрій для MFA.** Після вдалого другого кроку (`mfa/login-verify`) API кладе
HttpOnly-cookie `mfaTrust` (`MfaTrustCookie`, шлях `/api/auth`) з JWT-квитком окремої audience
`mfa-trust` на `Security:MfaTrustDays` (7). Квиток несе security stamp акаунта; `LoginUserCommandHandler`
пропускає другий крок, лише коли квиток чинний, виданий цьому акаунту і stamp не змінився — зміна
пароля чи скидання MFA ротує stamp і знімає довіру з усіх пристроїв. Вихід cookie не чіпає.

**Активація акаунта.** `POST onboarding/activate` відповідає `LoginUserResponse` і ставить refresh-cookie:
людина, яка щойно обрала пароль, потрапляє одразу в застосунок, а не на форму входу.

**Сайт.** `site/` — візитка й довідка на Astro + Starlight, окрема статика зі своїм деплоєм.
Довідка не пишеться на сайті: «Для користувача» — `docs/user/*.md`, «Для розробника» — `docs/dev/*`
(гайди, перенесені з Notion, і DevLog) плюс кореневі `ARCHITECTURE`, `CONTRIBUTING`, `BRANDBOOK`,
`SECURITY`, `docs/self-host/*`, які `site/scripts/sync-docs.mjs` збирає в `site/src/content/docs/`
перед `dev` і `build`, дописуючи frontmatter і переписуючи посилання між ними. Стиль — `site/src/styles/brand.css` з тими ж
токенами, що й `lileyka-theme.css`.

**Адреса відвідувача.** Рейт-ліміт входу, гео-блок і журнал зміни IP читають
`Connection.RemoteIpAddress`. Звідки він береться — `Security:ClientIp`: `Header` називає заголовок,
який пише єдиний проксі попереду (`CF-Connecting-IP` за Cloudflare у `Production`/`Staging`,
`X-Real-IP` від nginx у self-host bundle), і `ClientIpMiddleware` кладе його значення в адресу
зʼєднання одразу після `UseForwardedHeaders`; `TrustedProxies` — адреси або мережі, чий
`X-Forwarded-For` приймається. Без `Header` адреса — те, що лишив `X-Forwarded-For`, а він
приймається від будь-кого, поки список довірених проксі порожній. Запуск відмовляється від
`Jwt:Key`, коротшого за 32 символи або з шаблонним словом із прикладів (`JwtKeyRules`).

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
| `ProjectK.Architecture.Tests` | межі: залежності шарів, чужі репозиторії, події замість прямих викликів |
| `ProjectK.LoadTests` | навантаження |
| `Frontend/.../e2e` | Playwright проти docker-стека |

Бейслайн і команди перевірки — у [CONTRIBUTING.md](/dev/core/contributing/#перевірка).
