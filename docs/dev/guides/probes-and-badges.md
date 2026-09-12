---
title: Проби та вмілості
description: Модуль поступу — каталог з пакета, прогрес у базі, аудит змін.
sidebar:
  order: 6
---

Модуль відстежує складання **проб** і здобуття **вмілостей** членами куреня. Каталог (вимоги проб,
перелік вмілостей, зображення) не зберігається в базі: його віддає внутрішній пакет
`ProjectK.ProbeAndBadges.DependencyInjection`, який читає релізи `PlastBadgesParser`.

## Джерело каталогу

Секція `ProbeAndBadges` в `appsettings`: `DataSource` (`Auto` — спершу реліз із GitHub, потім
локальний кеш zip, потім вбудований seed), `VerifyReleaseZipChecksum`, `RequireReleaseZipChecksumAsset`.
Останній прапорець лишається `false`, поки реліз парсера не публікує checksum-файл — інакше каталог
не завантажиться взагалі.

## Контролери

- `ProbesCatalogController` — каталог проб;
- `BadgesCatalogController` — каталог вмілостей;
- `MemberProgressController` — поступ конкретного члена;
- черга розгляду вмілостей куреня — `KurinController.GetBadgeReviewQueue`.

## Сутності

- **ProbeProgress** — проба члена; **ProbePointProgress** — кожна точка проби з підписом того, хто
  її прийняв;
- **BadgeProgress** — вмілість;
- **ProbeProgressAuditEvent** / **BadgeProgressAuditEvent** — журнал змін.

Права: підписати точку може впорядник свого гуртка або провід куреня (`RolePermissionMap`,
ресурси `ProbeProgress` і `BadgeProgress` зі скоупом `OwnGroups` / `KurinWide`).

## Фронтенд

Екрани в `kurinModule`: `member-probe-page` (проба з точками), `skills-review-page` (черга на
розгляд), картка члена (`member-card`) із вкладками проби й вмілостей.
