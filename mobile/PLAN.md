# Лілейка Mobile — план для передачі між сесіями

> Статус: **лише планування. Код не написано.** Документ призначений для `mobile/PLAN.md` у репо `iamavasya/Project-K` (поза `docs/`, щоб не потрапляв на публічний сайт Starlight).
> Дата складання: 2026-10-10. База аналізу: HEAD `4f363805fd4a1738af990e5913ebd310fdf8ed56`.
> Позначки: ✅ перевірено по коду/документації, ⚠️ гіпотеза — перевірити спайком.

---

## 1. Контекст і мета

- Власник: Rost, .NET-інженер. Основна машина — **Windows**, **Mac немає**. Є **iPhone 16**.
- Мета: мобільний **додаток-компаньйон Лілейки**. Перший обсяг — **юнак** може виконати всі основні дії; далі поступове розширення до повної підтримки (лідери тощо).
- Потрібні **iOS і Android**, «дуже зручно і приємний UI», **максимально native-вигляд**, який можна витиснути з Ionic.
- Тестування: реальний **iPhone**; **Android на емулюваторі під Windows**. Фінальні/релізні збірки можуть іти в **CI**; dev/валідаційні — бажано без Mac (xtool-подібні тули).
- Ліцензія репо: Lileyka Source-Available License (некомерційна). Назва в UI — «Лілейка», «ProjectK» лише в коді/версії.

## 2. Рішення користувача (зафіксовані)

| # | Рішення |
|---|---|
| D1 | Окремий Ionic-застосунок у `mobile/` (в тому ж моно-репо) |
| D2 | Платформо-адаптивні режими: iOS-вигляд на iPhone, Material на Android |
| D3 | Шрифти/типографіка за брендбуком (Manrope; Neucha — лише лого і заголовки порожніх станів) |
| D4 | MVP-вкладки/екрани — спланувати детально (розділ 7) |
| D5 | Папки `mobile/ios`, `mobile/android` — ок |
| D6 | Адаптація токенів/auth на бекенді — «не проблема» |
| D7 | Фінальна збірка в CI допустима; dev-перевірки бажано швидкими тулами |

**Принцип дизайну:** *native behaviour, brand skin.* Платформа визначає навігацію, переходи, жести, пікери/шити, гаптику, safe areas. Бренд визначає кольори, шрифт, форму, порожні стани, тексти.

## 3. Огляд інструментів (результати дослідження)

- **xtool** (xtool-org/xtool, MIT): заміна Xcode на Linux/WSL/macOS; SwiftPM → iOS app, підпис, встановлення. Потрібен Xcode.xip (ліцензія Apple забороняє Xcode/SDK на не-Apple залізі — сіра зона). Без вивантаження в App Store на старті. Для `xtool setup` потрібен Team Key (App Manager).
- **xcross** (arxdeus/xcross): Flutter iOS **debug JIT** + hot reload з Windows/Linux; release/AOT неможливий без macOS (`gen_snapshot`). Для Ionic/Capacitor не підходить.
- **omarchy-apple-dev** (joshuaswarren, MIT): glue навколо xtool для нативного SwiftUI на Omarchy; `ship.sh` → .ipa + TestFlight через ASC API key (платний акаунт). Лише USB; без storyboard/xib; матриця Swift/Xcode SDK (FINDINGS.md).
- **Висновок:** жоден з них не прибирає потреби в **macOS CI-раннері** для гарантованого, коректного App Store-релізу Capacitor-додатку. xtool-лінія для dev — це **гейтований спайк (S4)**, не залежність.
- **Capacitor 8** ✅: iOS за замовчуванням SPM (`CapApp-SPM`), плагіни мають підтримувати SPM; конфіг `ios.path`/`android.path`, `server.url`, `server.cleartext` (Android блокує cleartext з API 28), `server.androidScheme` (default `https`), `server.iosScheme` (default `capacitor`); live reload: `cap run --live-reload`. Хмарні iOS-білдери (платні): Capgo Build, Capawesome Cloud, Odevio.
- **Ionic 9** ✅ (реліз 2026-08-19): Angular 18–22, zoneless за замовчуванням, імпорти з `@ionic/angular` (не `/standalone`), міграція `npx @ionic/migrate`. Ionic 8 — Angular ≤20. Angular 22 вимагає TypeScript 6.0 (web вже на 6.0.3).

## 4. Архітектура

### 4.1 Розкладка репо

```
Project-K/
  Backend/ Frontend/ docs/ site/ ...
  mobile/                  ← корінь Angular 22 + Ionic 9 + Capacitor 8
    src/ (app, environments, theme, assets)
    ios/                   ← capacitor ios (SPM)
    android/
    capacitor.config.ts
    PLAN.md                ← цей файл
```

`mobile/` — корінь проєкту, тож `ios/` і `android/` лежать поруч із `src/`, `ios.path` не потрібен.

### 4.2 Що ділимо з вебом

- Лише **чисті** файли: `*.dto.ts`, `*.enum.ts`, `*.function.ts`, `agenda.ts`, `me.dto.ts`, `greeting.function`, `day-label.function` тощо — через path alias (напр. `@lil/shared/*` → `../Frontend/projectk-frontend/src/app/...`).
- ⚠️ Перевірити, що ці файли не тягнуть UI-залежностей (Optimus/Tailwind/PrimeNG). `me.dto.ts` імпортує `scoreModule/models/score.enums` та `kurinModule/models/agenda` — це ✅ чисті типи.
- Тонкі мобільні API-сервіси (власні), без Optimus UI / Tailwind.
- Альтернатива, якщо alias болить: окремий пакет `shared/` з перенесенням (більший рефакторинг веба) — не в v1.

### 4.3 Мережа/конфіг

- Веб бере `apiUrl` з runtime `env.js` (`__PROJECTK_CONFIG__`); self-host — повноправний кейс. У мобілці — **вибір сервера** (екран 0): за замовчуванням хмарний, «Свій сервер» → валідація через `GET /health` (повертає версію).
- Збереження: токени/сервер — у **Keychain/Keystore** (Capacitor secure-storage плагін із SPM-підтримкою — ⚠️ вибрати і перевірити).
- Інтерсептор: Bearer access token, 401 → refresh (див. 8), помилки `{error, message}` → код у український текст; **ніколи** не показувати `HttpErrorResponse.message`.
- Контекст куреня: `POST api/auth/kurin-scope` перевидає токен.
- Фото: завжди даунскейл (API приймає ≤6 МБ на запит): `resizeToWidth`, `onlyScaleDown`, jpeg, `isImageTooLarge`.
- Zoneless: веб ще на zone.js, мобілка — zoneless (signals) ✅ узгоджено з Angular 22/Ionic 9.

## 5. Тема і бренд (BRANDBOOK v1.2)

Токени → Ionic CSS-змінні (`--ion-color-primary` + `-rgb/-shade/-tint/-contrast`, `--ion-background-color`, `--ion-text-color`, `--ion-border-color`, `--ion-toolbar-*`, `--ion-tab-bar-*`).

| Токен | Light | Dark |
|---|---|---|
| green (primary) | `#0E6E4E` | `#34C48D` |
| terracotta (one-spot) | `#D9762F` | `#EE9A5A` |
| ink | `#101413` | — |
| muted | `#6E7873` | — |
| line | `#E4E8E6` | — |
| surface | `#F7F9F8` | `#161B19` |
| paper | `#FFFFFF` | ground `#0D1110` |

Правила:
- Terracotta — «one-spot rule»: ніколи на кнопках/лінках/success. Непрочитане = primary-крапка, не terracotta.
- Шрифт: **Manrope** (variable woff2 локально: cyrillic/latin/latin-ext, ~112 kB; копія з `public/assets/fonts`). **Neucha** лише лого і заголовки порожніх станів.
- Шкала: h1 28px/800, body 15px, caption 13px, micro 11px uppercase.
- Рамки завжди 1px `--lil-line`; радіуси 8 (інтерактив) / 12 (картки); **без тіней**, окрім модалок/шитів/дропдаунів; анімація ≤200ms; hover = bg→surface (на таче не покладатись).
- Іконки лише PrimeIcons або Lucide (`lucide-static`), без емодзі і намальованих SVG. ⚠️ Перевірити `ion-icon` + Lucide (font/SVG). Дефолтні Ionicons у tab bar замінити.
- Порожні стани обов'язкові: заголовок Neucha без крапки + один пояснювальний рядок + одна дія. Каталог — BRANDBOOK §6, включно з офлайн «Звʼязку немає» і холодним стартом «Сервер ще прокидається».
- Одна primary-кнопка на екран. Тексти «ти», кнопки в інфінітиві, без «будь ласка/упс». Тости прості («Збережено»).
- Статуси задач: Todo=secondary(сірий) / InProgress=warn(terracotta) / Done=info(зелений) — джерело істини `agenda-status.config.ts`. Події календаря — приглушений синій `--lil-agenda-event-*`. RSVP: Going=зелений / Maybe=terracotta / NotGoing=сірий. Категорія — крапка з mix-кольорами `--lil-agenda-cat-{bg|line|dot}-mix`.
- Темна тема: `data-theme='dark'` + системна за замовчуванням; перемикач Системна/Світла/Темна.
- Захищене: табличка номера куреня (червоний `#b30003`/`#ff0005`, Arial) і `scouts-main.png` — не чіпати.
- Іконка застосунку: білий знак на зеленому `#0E6E4E` із теракотовою центральною пелюсткою.
- Конфлікти Ionic ↔ бренд, які треба явно перекрити: md-elevation (тіні), iOS large title vs h1 28px, дефолтні Ionicons, радіуси кнопок.
- Режими Ionic: `mode: 'ios'` на iPhone, `'md'` на Android (автовибір платформою). У браузері перемикання для перевірки — ⚠️ параметр `?ionic:mode=`.

## 6. Телефонні уроки (CONTRIBUTING «Телефон»)

Без tooltip/hover-only; рядок `(click)` + реальний лінк; оверлеї; фото даунскейлити; помилки через коди; використовувати патерни `UserActionService` (спінер/захист від подвійного кліку) та `RequestFeedbackInterceptor` (`requestFeedback('silent'|'errors',[handled])`) — у мобілці реалізувати еквіваленти.

## 7. Детальний план екранів MVP («юнак»)

**Таби (5): Головна · Календар [Події | Задачі] · Поступ (лише якщо `hasYouthProgram`) · Сповіщення · Ще.**

### 7.0 Auth (без табів)
1. Вибір сервера (хмара за замовчуванням / «Свій сервер» з валідацією `/health`).
2. Логін; помилки код→текст (як `LOGIN_ERROR_TEXT` у `features/authModule/pages/login/login.ts`).
3. Крок MFA-коду (`mfaToken`); еквівалент `mfaTrust` (7 днів) — device-bound (розділ 8).
4. Forgot-password / активація — через веб у in-app browser, потім universal links.
5. Банер холодного старту; біометрія — пізніше. **Реєстрацію (`join`) у v1 не робимо.**

### 7.1 Головна (`GET api/me`)
- Large title-привітання (`greeting.function`, `day-label.function`), підрядок: дата · ступінь · гурток.
- Картки: **Найближче** (14 днів, спершу ті, що чекають відповіді; інлайн-сегмент «Іду/Можливо/Не йду» + гаптика); **Мої задачі** (swipe Почати/Зроблено, прострочені червоним, посилання «Дошка»); **Проба** (прогрес + 3 наступні пункти); **Вмілості** (три полиці: На перевірці/У роботі/Підтверджені); **Точкування** (сума, джерела, місце X з N); **Вкладка** (сплачено — зелений / борг — червоний / надлишок «+», квартал, ставка).
- Прибрати з Головної плитки «Мій профіль»/«Мої курені» (переїжджають у «Ще», аватар у хедері). «Справи» (лідерські) — фаза 3.
- Збережене розташування плиток: `/user/me/layouts` (по `boardKey`) — спершу read-only, поважати приховані.
- Pull-to-refresh, скелетони, останній снапшот офлайн; підпис куреня на рядках, якщо куренів >1 (`MyKurinRefDto.namedAfter`).

### 7.2 Календар → Події
- Тижнева стрічка + список по днях; `ion-datetime` з `highlightedDates` для стрибка.
- Перемикач «Графіки гуртків» (бліді рядки, без RSVP).
- Рядок: крапка категорії (mix-кольори), локація, чіп RSVP.
- Деталі — `ion-modal` з breakpoints ~[0, .5, .92]: RSVP (з приміткою для серій), посилання на мапу. Додати в календар пристрою і створення подій — пізніше.

### 7.3 Календар → Задачі
- Фільтр-чіпи Зробити / В процесі / Зроблено (замість канбану), «Моє» за замовчуванням увімкнено, пошук, сортування (action sheet), infinite scroll («Завантажити ще»).
- Тег статусу за брендом; PerMember-прогрес «5 з 8».
- Дії гейтяться `canChangeStatus` (swipe + деталь-шит). Архів у MVP немає.

### 7.4 Поступ
- Сегменти **Проба | Вмілості**.
- Проба: хедер прогресу, тег статусу, акордеон за `sectionCode`, пункти read-only з хто/коли (повний список потребує окремого endpoint — `MyProbeDto.nextPoints` лише найближчі). Підписують лише ментори.
- Вмілості: три полиці з картинкою/статусом/датою перевірки; відхилена показує коментар. «Подати вмілість» через пошуковий каталог — фаза 2b. Порожній стан напр. «Вмілостей ще немає».

### 7.5 Сповіщення
- Список по днях; непрочитане = primary-крапка; swipe «прочитано»; тап → мапінг на мобільний маршрут (`AppNotificationType`, 12 типів; поле `route` — це веб-маршрут, потрібна таблиця відповідності; fallback — in-app browser); бейдж на табі.
- Push — фаза 3 (device-token endpoint, APNs-ключ, FCM).

### 7.6 Ще
Профіль (read-only картка учасника) · Мої курені (перемикач, «ЗАРАЗ», `POST api/auth/kurin-scope`) · Вкладка · Точкування (+ таблиця групи) · Акаунт і безпека (імʼя/email/пароль/MFA/сесії; MVP — через in-app browser для MFA) · Вигляд (Системна/Світла/Темна) · Сервер · **Про Лілейку** (вимога ліцензії, без змін, з атрибуцією) · Конфіденційність · Повідомити про проблему (існуючий feedback endpoint) · Версія · Вийти.

## 8. Зміни на бекенді (Backend, під ворота тестів)

1. **Мобільний refresh-потік**: зараз refresh — httpOnly cookie `refreshToken` (`Path=/api/auth`, SameSite=None/Lax залежно від HTTPS, `RefreshTokenCookie.cs`). Для мобілки — токен у тілі відповіді за opt-in заголовком, зберігання в Keychain/Keystore.
2. **CORS**: зараз один origin (`EnvCorsOrigin` + `AllowCredentials`, Program.cs ~L150). Потрібен allow-list (origins застосунку `capacitor://localhost`, `https://localhost`, dev-origins). ⚠️ Поведінка cookie у WKWebView з `capacitor://localhost` не перевірена — тому non-cookie шлях.
3. **Device-bound MFA trust** (аналог `mfaTrust` cookie, 7 днів, security-stamp bound).
4. **Push device registration** (фаза 3).
5. **Universal/app links** association-файли (лише хмарний домен).
6. **Тестовий акаунт для рев'ю в магазинах** (у Production демо-акаунтів немає ✅ ARCHITECTURE.md).
7. Нові ендпоїнти додавати в `AuthorizationBaselineMatrixTests`, відповідати `ProjectK.Architecture.Tests`; вертикальні слайси, іменування `…Command/Query/Handler/Validator`.
8. Повний список пунктів проби для юнака (read-only) — окремий endpoint.

## 9. Матриця тестування

| Ціль | Спосіб |
|---|---|
| Ітерація UI | Chrome DevTools, обидва режими Ionic |
| iPhone без нативної збірки | Safari по Tailscale (той самий WebKit; плагіни/status bar не перевіряються); патерн `ng serve --configuration tailscale --host 0.0.0.0` уже є у вебі |
| Android | Емулятор на Windows: `cap run android --live-reload`, `server.url=http://10.0.2.2:4200`, `server.cleartext=true`, бекенд `http://10.0.2.2:5205/api`, CORS має дозволяти dev-origins ⚠️ (віртуалізація Windows — перевірити) |
| iPhone нативно (гарантовано) | CI macOS → TestFlight |
| iPhone нативно (швидко) | Спайк S4: xtool/WSL2 (usbipd-win для USB ⚠️) |

## 10. Фази і гейти

**Фаза 0 — спайки (кожен має критерій pass/fail):**
- **S1** Ionic 9 + Angular 22 + Capacitor 8 hello у `mobile/` (zoneless) + Android-емулятор + live reload. *Pass:* екран на емуляторі, HMR працює.
- **S2** iPhone Safari по Tailscale. *Pass:* UI відкривається на iPhone, ios-режим коректний.
- **S3** CI macOS → TestFlight порожньої оболонки. *Pass:* білд в TestFlight з GitHub Actions. Перевірити ліміти/вартість macOS-раннерів.
- **S4** (ризиковий, опційний) xtool/WSL2 оболонка на iPhone. Проблеми: Capacitor-шаблон має Main/LaunchScreen storyboard, xtool їх не підтримує → програмний `CAPBridgeViewController` + `UILaunchScreen`; лише SPM-плагіни; Xcode.xip ліцензійна сіра зона. *Fail ≠ блокер.*

**Фаза 1 — фундамент:** маппінг теми, shell з табами, логін + вибір сервера + secure storage, shared alias, мобільні зміни auth на бекенді, CI.

**Фаза 2 — MVP «юнак»:** 2a Головна / Календар+Задачі / Сповіщення / Ще; 2b Поступ (з поданням вмілості); 2c безпека акаунта.

**Фаза 3 — розширення:** push (APNs/FCM), біометрія, камера/фото, офлайн-кеш, лідерські функції (аркуш відвідуваності — природний телефонний кейс; плитка «Справи»; підпис проб; модерація вмілостей; створення подій).

## 11. CI/CD

- `mobile.yml` за зразком наявних path-filtered workflow (`angular.yml` — Frontend/**, Node 22, `sed`-ін'єкція версії): ubuntu-job — lint/test/web build + Android AAB; macOS-job — iOS archive + fastlane/TestFlight на release або `workflow_dispatch`.
- Секрети: ASC API key (`.p8`), Android keystore.
- Версія: плейсхолдери `v0.0.0-dev` / `LocalDevelopment` у `mobile/src/environments/environment*.ts` (як у вебі). Розширити composite action `.github/actions/release-info` (VERSION/CODENAME; regex тегів `^[0-9]+\.[0-9]+(\.[0-9]+)?([-.][0-9A-Za-z.-]+)?$`): iOS-версія має бути **числовою** (теги на кшталт `v0.20.0-beta` не підходять) + build number з run number.
- Розглянути path-фільтри так, щоб зміни спільних DTO у вебі тригерили mobile build.
- PR-гейту немає (один мейнтейнер, CODEOWNERS `* @iamavasya`).

## 12. Ризики і пастки

1. Cookie-refresh + один origin у CORS vs Capacitor origins → non-cookie шлях (розділ 8).
2. `apiUrl` runtime-конфіг → потрібен вибір сервера.
3. Нечислові версії тегів vs iOS.
4. Storyboard-и vs xtool (S4).
5. Плагіни лише з SPM.
6. App Review: потрібен робочий тестовий акаунт; користувачі-неповнолітні → target audience / age rating, privacy policy URL, правила видалення акаунта при in-app реєстрації (тому без `join` у v1).
7. MFA QR на тому ж телефоні → ручний ключ / `otpauth://`; у MVP — in-app browser.
8. `route` у сповіщеннях — веб-маршрути → мапінг.
9. Ionic 9 новий (відставання екосистеми).
10. Конфлікти бренду з Ionic (тіні, large title, Ionicons).
11. Ліцензія: сторінка «Про Лілейку» з атрибуцією **має лишатись у кожній працюючій копії**; назва «Лілейка» зафіксована; сторонні не можуть модифікувати.
12. Xcode.xip-ліцензія (S4).

## 13. Відкриті питання

- Чи показувати «Поступ» не-юнакам (зараз ховаємо без `hasYouthProgram`)?
- Чи поважати збережений layout плиток (read-only) у v1?
- Обсяг push у фазі 3 (які типи сповіщень).
- Target audience / age rating у сторах.
- Оформлення Apple Developer акаунта (платний) і Google Play.
- Вибір secure-storage плагіна з SPM-підтримкою.

## 14. Що перевірено, а що ні

✅ Перевірено: Capacitor/Ionic можливості (док), код і структура репо, auth-cookie поведінка, CORS, CI, BRANDBOOK, CONTRIBUTING, форма `api/me`, моделі agenda/notification.
✅ Перевірено спайками (див. §17): Angular 22 zoneless + Ionic 9 + Capacitor 8 збирається і працює на Android та iOS-симуляторі; `10.0.2.2`+cleartext у Capacitor 8 (live reload і dev-API на емуляторі).
⚠️ Не перевірено: WKWebView-cookie з `capacitor://localhost`; WSL2+usbipd з iPhone; ATS при http; `ion-icon`+Lucide; `?ionic:mode=`; віртуалізація Android-емулятора на Windows; чистота shared DTO від UI-імпортів; `auth.interceptor.ts` прочитано не повністю.

## 15. Список для читання наступній сесії

- `ARCHITECTURE.md`, `BRANDBOOK.md` (особливо §2 токени, §6 порожні стани), `CONTRIBUTING.md` (розділ «Телефон»), `LICENSE`.
- `Frontend/projectk-frontend/`: `package.json`, `angular.json`, `src/environments/environment*.ts`, `src/lileyka-theme.css`, `src/app/lileyka-preset.ts`, `src/app/app.routes.ts`, `public/assets/fonts/*`.
- `features/dashboardModule/models/me.dto.ts`, `services/me.service.ts`, `functions/greeting.function.ts`, `day-label.function.ts`.
- `features/kurinModule/models/agenda.ts`, `agenda-status.config.ts`, `services/{agenda-service,probes-and-badges,client-cache,membership-service}`.
- `features/notificationsModule/models/app-notification.model.ts`.
- `features/authModule/pages/login/login.ts`, `services/auth.interceptor.ts` (дочитати).
- `shared/tile-board/tile-layout.service.ts`, `shared/empty-state`.
- Backend: `ProjectK.API/Program.cs` (CORS), `Helpers/RefreshTokenCookie.cs`, `MfaTrustCookie.cs`, `AuthorizationBaselineMatrixTests`, `ProjectK.Architecture.Tests`.
- `.github/actions/release-info/action.yml`, `.github/workflows/{angular,dotnet,site,e2e,selfhost-docker}.yml`.
- `docs/user/features/*` (dashboard, calendar-and-tasks, probes-and-badges, notifications, dues, score, member-card), `docs/user/roles/*`, `docs/user/account.md`.

## 16. Перший крок наступної сесії

Не починати з коду фіч. Спершу S1 (+S2) у `mobile/`; після них узгодити з користувачем результат і лише тоді Фаза 1. Пристроєві перевірки (iPhone, емулятор) виконує користувач; у середовищі агента доступ до репо лише на читання — зміни віддавати патчем/файлами.

## 17. Результати Фази 0 (2026-10-10)

Каркас у `mobile/`, PR #114 (draft, у `dev` не мерджиться до рішення користувача). Перевірки ганяє `.github/workflows/mobile.yml` на кожен PR, що чіпає `mobile/**`; скріни в артефактах `mobile-screens` і `ios-screens`.

- **S1 — ✅ pass.** Ionic 9 + Angular 22 (zoneless, без zone.js) + Capacitor 8. На емуляторі Android (API 35, GitHub Actions + KVM) `scripts/emulator-smoke.sh` перевіряє: рендер, режим `md`, платформа `android`, лічильник (сигнали), вкладка «Ще», темна тема за системою, live reload з `10.0.2.2:4200` (правка `home.ts` долітає на емулятор) і dev-API `http://10.0.2.2:5205/api`. Cleartext дозволено лише для `10.0.2.2`/`localhost` через `network_security_config.xml`.
- **S2 — ✅ pass (замінено).** Замість iPhone по Tailscale: iOS-симулятор на macOS-раннері. Нативна збірка Capacitor (SPM, без підпису) встановлюється і запускається (режим Ionic `ios`, платформа Capacitor `ios`, темна тема за системою); Safari на симуляторі відкриває dev-сервер (Головна, Ще) і сам вмикає режим `ios` без `?ionic:mode=`. Перевірка на справжньому iPhone по Tailscale лишається за користувачем.
- **S3 — ⏸ заблоковано.** Потрібні Apple Developer акаунт, App Store Connect API key і сертифікати в секретах репо. macOS-раннер для симулятора вже працює (~10 хв на прогін).
- **S4 — ⏸ заблоковано.** Потрібні iPhone і Windows/WSL2 користувача.

Що з'ясувалося по дорозі:
- Angular CLI 22 вимагає Node ≥22.22.3 або ≥24.15.
- Sass `@import` застарів, теми підключаються через `@use`.
- SonarCloud (quality gate) вимагає: `npm ci --ignore-scripts`, без `npx` у workflow, закріплені SHA сторонніх actions, `Package.resolved` для SPM, вузький `FileProvider`, `allowBackup=false`, явний `usesCleartextTraffic=false`.

Наступний крок: користувач дивиться скріни і PR, вирішує по S3/S4, далі Фаза 1.

## 18. Поворот на PWA (2026-10-10)

Рішення користувача: поки нативні збірки чекають (S3/S4 потребують комп'ютера), основний шлях —
максимально «нативна» PWA з того самого `mobile/`. Ionic сам обирає вигляд і в браузері: `ios` на
iPhone/iPad, `md` (Material) на Android.

Зроблено:
- маніфест, іконки Лілейки (any, maskable, apple-touch), iOS meta-теги, `theme-color` за системною темою;
- Angular service worker лише в браузерній прод-збірці (у нативних оболонках вимкнений);
- картка встановлення: кнопка «Встановити» на Android (`beforeinstallprompt`), підказка
  «Поділитися → На початковий екран» на iPhone;
- haptics на вебі через `navigator.vibrate` (Android), на iPhone без вібрації;
- PWA-смоук у Playwright (`npm run e2e`, job `pwa` у CI): маніфест та іконки, `ios`/`md`, таби,
  темна тема, запуск з головного екрана, офлайн-перезавантаження (Chromium).

Відкрито:
- хостинг PWA: вирішено — той самий домен, шлях `/m/` (CORS, cookie і `env.js` спільні з вебом; self-host
  без окремого домену). Хмара: `angular.yml` збирає PWA в `dist/.../m`, маршрути в `staticwebapp.config.json`;
  self-host: стадія `mobile` у web-Dockerfile і `location /m/` в nginx. Для фото пізніше треба дозволити
  `camera` у `Permissions-Policy` для `/m/`;
- вхід у PWA (2026-10-10): екран входу (email + пароль, другий крок з кодом або кодом відновлення),
  вихід у «Ще». Бекенд без змін: PWA на тому ж origin, тому бере ту саму refresh-cookie і той самий
  `authState` у localStorage, що й веб (вхід/вихід в одному діє в іншому в тому самому браузері).
  Офлайн сесія не скидається. Перевірено в CI: мок API в `pwa` і справжній API в job `pwa-real-api`
  (e2e-стек). Нативні оболонки поки без входу: їм потрібен refresh-токен у тілі (§8). На preview SWA
  вхід не працює: staging API дозволяє в CORS лише основний origin SWA. Ще не зроблено: обов'язкове
  налаштування MFA для привілейованих акаунтів (у вебі це діалог після входу);
- push (APNs через Web Push на iOS 16.4+ лише для встановленої PWA) — Фаза 3.
