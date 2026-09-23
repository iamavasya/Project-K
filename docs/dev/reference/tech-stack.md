---
title: Технологічний стек
description: З чого зібрана Лілейка — фронтенд, бекенд, внутрішні пакети, інфраструктура.
sidebar:
  order: 1
---

Стан на вересень 2026 (гілка `1.0.0`).

## Фронтенд

- **Angular 22**, standalone-компоненти, сигнали (декораторів `@Input`/`@Output` не лишилось).
- **@openng/optimus-ui** — MIT-форк PrimeNG 21. PrimeNG з v22 став комерційним, тож застосунок
  живе на спільнотному форку; компоненти й API ті самі.
- **TailwindCSS 4** для розкладки, візуальна система — токени з `lileyka-theme.css`
  (див. [Брендбук](/dev/core/brandbook/)).
- **TypeScript 5.9**, **RxJS 7.8**.
- `ngx-image-cropper` — обрізання фото, **FullCalendar** (MIT-плагіни) — календар, **Quill** через
  `p-editor` — текст звіту про проблему.
- Тести: **Karma / Jasmine**; e2e — **Playwright** проти контейнерного стеку `e2e`.

## Бекенд

- **ASP.NET Core (.NET 10)**.
- **EF Core 10** + **SQL Server**.
- **MediatR** — CQRS: кожна дія — `Command`/`Query` з хендлером, вертикальні зрізи в `Features/`.
- **AutoMapper** з профілями на модуль.
- **ASP.NET Identity** + **JWT Bearer**; MFA (TOTP) для проводу; refresh-сесії в `UserRefreshTokens`.
- **Serilog** — структуровані логи (File, Application Insights, енрічер, що ховає чутливе).
- **QuestPDF** — PDF-звіти куреня.
- **Azure.Storage.Blobs** — фото й файли (локально — Azurite).
- **Resend** — пошта (локально — `Mock`; на staging усе перенаправляється на `delivered@resend.dev`).
- **GitHub Issues** — звіти про проблеми з застосунку (`Feedback:GitHub`, мітки `bug`, `from client`);
  без токена — в лог.
- **Swashbuckle** — Swagger / OpenAPI.
- Тести: **xUnit**, **FluentAssertions**, **Moq**; архітектурні тести на іменування й межі модулів.

## Внутрішні NuGet-пакети

- `ProjectK.Optimization` — закритий пакет, на якому працює планування таборів.
- `ProjectK.ProbeAndBadges.DependencyInjection` — каталог проб і вмілостей, дані беруться з релізів
  `PlastBadgesParser`.

Обидва публікуються в GitHub Packages, тому для збірки потрібен `NUGET_AUTH_TOKEN`.

## Інфраструктура

- **Docker Compose** (`docker/`) — стек API + Web, керується `scripts/dev.sh`. Один образ .NET
  обслуговує `dev | e2e | selfhost | tailscale | staging | prod` через `ASPNETCORE_ENVIRONMENT`.
- **Azurite** — локальна емуляція Azure Blob Storage; **SQL Server** у контейнері, спільний для всіх
  середовищ.
- **Self-host bundle** — один порт через nginx з проксі `/api`, `/badges_images`, `/blob`.
- Прод: API — Azure App Service за Cloudflare, фронтенд — Azure Static Web Apps; staging — той
  самий набір в окремій підписці. **CI** — GitHub Actions: `dotnet.yml`, `angular.yml`, `e2e.yml`,
  `site.yml`, `selfhost-docker.yml`, `codeql.yml`; Dependabot і secret scanning.
- Сайт, довідка й статичне демо — **Astro + Starlight** (`site/`) на Cloudflare Pages; діаграми в
  довідці — **Mermaid** (`astro-mermaid`), рендер у браузері.
