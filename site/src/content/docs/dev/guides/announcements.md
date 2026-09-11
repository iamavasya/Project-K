---
title: Публічні анонси
description: Чернетки анонсів із життєвим циклом і модерацією.
sidebar:
  order: 9
---

Публічні анонси працюють через **чернетки** з життєвим циклом і модерацією. Контролер —
`PublicAnnouncementsController` (`InfrastructureModule`), сутність — `PublicAnnouncementDraft`.

## Життєвий цикл

`створення` → `редагування` → `preview` → `submit` (на модерацію) → `approve` / `reject` → `publish`.

- `POST /` — створити чернетку; `PUT /{draftKey}` — редагувати; `DELETE /{draftKey}` — видалити;
- `POST /{draftKey}/preview` — попередній перегляд;
- `POST /{draftKey}/submit` — надіслати на модерацію;
- `POST /{draftKey}/approve` · `POST /{draftKey}/reject` — рішення модератора;
- `POST /{draftKey}/publish` — опублікувати;
- `GET /` — список; `GET /cleanup-status` — статус очищення застарілих чернеток.

## Зображення

- `POST /image` — завантажити (Azure Blob, локально Azurite); приймаються лише зображення;
- `GET /image/{imageKey}` — віддати; `DELETE /image/{imageKey}` — видалити.

У сайдбарі є терракотова крапка «є що модерувати», коли є чернетки на розгляді — це одне з
небагатьох місць, де брендбук дозволяє терракоту як статус.
