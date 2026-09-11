<img width="1080" height="288" alt="Лілейка" src="https://github.com/user-attachments/assets/15042aba-251b-4551-b91c-0734dada96d6" />

# ProjectK · Лілейка

![GitHub Release](https://img.shields.io/github/v/release/iamavasya/Project-K?include_prereleases)
![GitHub last commit](https://img.shields.io/github/last-commit/iamavasya/Project-K)
![GitHub Release Date](https://img.shields.io/github/release-date-pre/iamavasya/Project-K)
[![.NET](https://github.com/iamavasya/Project-K/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/iamavasya/Project-K/actions/workflows/dotnet.yml)
[![Angular CI](https://github.com/iamavasya/Project-K/actions/workflows/angular.yml/badge.svg?branch=main)](https://github.com/iamavasya/Project-K/actions/workflows/angular.yml)

**ProjectK** — кодова база **Лілейки**, системи для куреня УПЮ (Пласт — Національна скаутська
організація України). Весь курінь в одному місці: реєстр, проби й вмілості, календар і планування.
Зроблено пластуном для пластунів.

> Назви: продукт і інтерфейс — **Лілейка**; репозиторій, образи Docker і CI — **ProjectK**.
> Див. [`BRANDBOOK.md`](BRANDBOOK.md) §0.

## Що вміє

- **Реєстр** — люди з фото, ступенями, відзначеннями і пересторогами; гуртки, провід і КВ з
  історією урядів; людина може стояти в кількох куренях. Імпорт складу з Excel, PDF-звіт куреня.
- **Проби й вмілості** — каталог з офіційних програм, точки з підписом, черга розгляду вмілостей,
  журнал змін.
- **Календар і задачі** — сходини, табори й заходи з призначенням «для кого» (курінь → гурток →
  людина), повторами й відповідями; дошка задач над тими самими подіями.
- **Планування** — підбір дати табору за зайнятістю кадри виховників, результат одразу в календар.
- **Сповіщення** — внутрішня скринька: верифікації, зміни в календарі, розгляд вмілостей, провід.
- **Права з урядів** — доступ виводиться з уряду в курені (Звʼязковий, впорядник, курінний…), а не з
  окремих налаштувань; кожна дія перевіряється на сервері за ресурсом і скоупом.
- **Безпека** — двофакторний вхід з довіреним пристроєм, сесії з відкликанням, рейт-ліміти,
  гео-блок через заголовок Cloudflare, аудит.
- **Self-host** — до кожного релізу додається Docker bundle: один порт, майстер першого запуску.

## Стек

- **Бекенд:** .NET 10, ASP.NET Core, EF Core (SQL Server), MediatR, Serilog → Application Insights.
- **Фронтенд:** Angular 22 (standalone, сигнали), @openng/optimus-ui + Tailwind, дизайн-система
  «Лілейка».
- **Сховище:** Azure Blob Storage (локально — Azurite) для фото й файлів.
- **Хостинг:** Azure App Service (API) + Static Web Apps (web) за Cloudflare.
- **Сайт і довідка:** Astro + Starlight у `site/`.

## Швидкий старт

### Self-host (Docker)

До кожного релізу додається bundle. Повний гайд — [`docs/self-host/README.md`](docs/self-host/README.md).

```bash
cd projectk-<версія>-docker-selfhost
cp .env.example .env     # секрети, адреса, назва
docker compose up -d     # http://localhost:8080 → майстер першого запуску
```

### Локальна розробка

Потрібні **.NET 10 SDK**, **Node ≥ 22.22.3** і Docker (SQL Server + Azurite).

```bash
./scripts/dev.sh tools up          # спільні SQL і Azurite, один раз
./scripts/dev.sh up dev --build    # API + web: http://localhost:4200, API 5205
./scripts/dev.sh watch dev         # те саме з hot-reload
```

Або окремо:

```bash
dotnet run --project Backend/ProjectK.Backend/ProjectK.API
cd Frontend/projectk-frontend && npm ci && npm start
```

Довідка для розробника — [`site/`](site/README.md) → «Для розробника» (запуск, стек, сутності,
міграції, DevLog).

## Структура репозиторію

```
Backend/ProjectK.Backend/   рішення .NET (API, BusinessLogic, Infrastructure, Common, tests)
Frontend/projectk-frontend/ застосунок Angular (бренд-ассети в public/assets/)
site/                       сайт і довідка (Astro + Starlight)
docs/user/  docs/dev/       джерела довідки для користувача й розробника
docker/                     compose-стеки, nginx, шаблони env, self-host bundle
scripts/                    dev.sh та інші помічники
ARCHITECTURE.md             з чого складається система і куди йде запит
CONTRIBUTING.md             конвенції, за якими пишеться новий код
BRANDBOOK.md                візуальна система (§0 — перед будь-якою зміною UI)
SECURITY.md                 як повідомити про вразливість
```

Планування навмисно живе поза репозиторієм (`todo/`, у `.gitignore`); те, що переживає задачу,
потрапляє в документи вище.

## Посилання

- [Релізи](https://github.com/iamavasya/Project-K/releases)
- [Бренд-ассети](Frontend/projectk-frontend/public/assets/README.md)
- [Self-host](docs/self-host/README.md)
- [Політика безпеки](SECURITY.md)
- [DevLog](https://iamavasya.notion.site/2547512d9a72800ba996d95fd4f61a73) — щоденник розробки
