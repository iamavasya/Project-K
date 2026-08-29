# Конвенції ProjectK

Цей файл описує **один патрон**, до якого зведено код у релізі 0.19.0. Він існує тому, що
розбіжності розповзаються тихо: до 0.19.0 у бекенді співіснували чотири розкладки модулів, а на
фронті — чотирнадцять класів розбіжностей іменування. Якщо нова конвенція потрібна — спершу зміни
цей файл, потім код.

---

## Бекенд

### Шари

```
ProjectK.Common          → (нічого)
ProjectK.Infrastructure  → Common
ProjectK.BusinessLogic   → Common
ProjectK.API             → Common, BusinessLogic, Infrastructure
```

`Infrastructure` і `BusinessLogic` **не знають одне про одного**. Коли бізнес-логіці треба щось із
зовнішнього світу — інтерфейс оголошується в `Common`, а реалізується в `Infrastructure`
(наприклад `IKurinReportSource`, `IPublicAnnouncementImageStore`, `IAppUserRepository`).

Що де лежить:

| Шар | Що | Чого там не буває |
|---|---|---|
| `Common` | сутності, інтерфейси, DTO, налаштування, чисті правила (`RolePermissionMap`, `AgendaVisibility`) | ASP.NET-типи (`IFormFile`, `ControllerBase`), EF-типи |
| `Infrastructure` | EF, репозиторії, блоби, пошта, логери, фонові служби, сідинг, рендеринг PDF | доменні рішення |
| `BusinessLogic` | хендлери, доменні служби, мапінг-профілі | `Microsoft.EntityFrameworkCore`, HTTP |
| `API` | контролери, middleware, серіалізація, DI-хост | доменна логіка, прямі звернення до `AppDbContext` |

**Папки `Services/` в API немає і не має з'являтися.** Якщо новий клас тягне на «сервіс» — його
місце в `BusinessLogic` (рішення) або `Infrastructure` (зовнішня система).

Кожен шар реєструє **своє**: `AddInfrastructure()`, `AddBusinessLogic()`; в API лишається тільки
хостове (`HttpContextAccessor`, прив'язка конфігурації, `TimeProvider`, `ICurrentUserContext`).

### Вертикальні зрізи

Уся бізнес-логіка живе за одним шаблоном:

```
Modules/<Module>/Features/<Entity>/<Verb>/
    <Verb><Entity>Command.cs        // або Query
    <Verb><Entity>CommandValidator.cs
    <Verb><Entity>CommandHandler.cs
```

Запит, валідатор і хендлер лежать **поруч**. Окремих папок `Commands/`, `Queries/` чи `Handlers/`
не буває — саме їхнє співіснування й давало чотири різні розкладки.

Поза `Features/` у модулі допускаються лише `Models/` (типи відповідей) і `Services/` (доменні
служби, спільні для кількох зрізів).

### DTO проти Records

- **`Common/Models/Dtos/<Module>/`** — усе, що перетинає межу API: те, що клієнт отримує.
  Запити — у `<Module>/Requests/`.
- **`Common/Models/Records/`** — внутрішні типи-значення, які **ніколи не серіалізуються клієнту**:
  `ServiceResult`, `ResourceAccessDecision`, `AgendaViewerScope`, результати завантаження блобів.

Перевірка проста: якщо тип згадується у відповіді або запиті контролера — це DTO.
(`DateRangeDto` довго лежав у `Records/`, хоч і був у відповіді планування.)

### Репозиторії

Успадковують `BaseEntityRepository<T>`, який дає CRUD; ключ береться з моделі EF, тож сутність не
мусить називати його однаково. Перевизначай лише те, що справді відрізняється — eager loading,
звужений запит, свідома відмова від `GetAllAsync`.

Репозиторії групуються за модулем (`Repositories/<Module>/`) і доступні **тільки** через
`IUnitOfWork` — окремо вони не реєструються, бо це створювало б другий екземпляр на той самий
`DbContext`.

### Відповіді та помилки

Кожна невдача повертається як `{ "error": "<StableCode>", "message": "<English text>" }`.
У хендлері це `ServiceResult<T>.Failure(type, code, message)`, у контролері — `this.Failure(...)`.
Текст помилки **не кладеться в `Data`**.

`error` — стабільний код, за яким UI обирає формулювання. `message` англійською і призначений
розробнику; користувацький текст пише фронт (див. `LOGIN_ERROR_TEXT`).

Усі `DateTime` серіалізуються як UTC із `Z` — це робить `UtcDateTimeConverter`, вручну позначати
`DateTimeKind` не треба.

### Час

`TimeProvider` впорскується там, де час **вирішує**: терміни дії токенів і запрошень, вікна
перестороги, діапазон агенди за замовчуванням. Для простих позначок часу (`CreatedAtUtc`,
логування, сідери) далі використовується `DateTime.UtcNow` — інʼєкція там нічого не дає.

### Мапінг

Один спосіб — **AutoMapper**, профілі в `BusinessLogic/MappingProfiles/`. Мапінг у DTO має бути
повним; часткові мапінги в сутність позначаються `MemberList.None` із поясненням, бо решту полів
заповнює хендлер. `MappingConfigurationTests` не дасть додати мапу з незаповленим полем.

Складні відповіді, які збираються з кількох джерел із довідниками (логін, агенда, звіт), лишаються
явними фабриками — профіль їх чесно не виражає.

### Кодування

Усі файли — UTF-8. (Один файл довго лежав у cp1251 і мовчки випадав з усіх автоматичних правок.)

---

## Фронтенд

### Іменування

| Що | Як | Не так |
|---|---|---|
| Файл компонента | `member-card.ts` | `member-card.component.ts`, `login-component.ts` |
| Шаблон і стилі | `member-card.html`, `member-card.css` | `member-card.component.html` |
| Клас компонента | `MemberCardComponent` | `MemberCard` |
| Файл сервіса | `agenda.service.ts` у папці `agenda-service/` | `agenda-service.ts` |
| Папка фічі | `notificationsModule/` | `notifications/` |
| Стилі в декораторі | `styleUrl: './x.css'` | `styleUrls: ['./x.css']` |

Єдиний свідомий виняток — кореневий клас `App` у `app.ts`: так його генерує сам Angular і так на
нього посилається бутстрап.

Суфікс `Component` лишається **в класі**, але не у **файлі**: ім'я файла й так лежить у папці, що
його називає, а в шаблоні та імпортах суфікс відрізняє компонент від сервіса чи моделі.

### Помилки з API

Бекенд віддає `{ error, message }`. UI мапить **код** на свій текст і ніколи не показує
`HttpErrorResponse.message` — Angular завжди заповнює його рядком на кшталт
`"Http failure response for ..."`, тож фолбек на нього гарантовано показує користувачу службовий
текст замість людського.

---

## Перевірка

Бейслайни, які має тримати кожна зміна:

```bash
dotnet test Backend/ProjectK.Backend/ProjectK.Backend.sln
```

- бекенд — **740** тестів
- фронт — **615** тестів, лінт **0 помилок** (18 попереджень — поточний бейслайн, див. `docs/quality-baseline.md`)
- e2e — **97** тестів

```bash
cd Frontend/projectk-frontend && npx ng lint && npx ng test --watch=false --browsers=ChromeHeadless && npx ng build
```

E2E піднімаються через докер:

```bash
./scripts/dev.sh tools up && ./scripts/dev.sh up e2e --build
```

> Відомо: набір e2e нестабільний — приблизно один тест на прогін, щоразу інший. Причина не в
> бекенді, а в анімації діалогів (маска `p-dialog-mask` перехоплює клік). Перш ніж вважати провал
> регресією, прожени спек ізольовано двічі.
