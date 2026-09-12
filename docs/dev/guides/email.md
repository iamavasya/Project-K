---
title: Пошта
description: Як система надсилає листи, які є провайдери й де що налаштовується.
sidebar:
  order: 5
---

Абстракція — інтерфейс `IEmailService` у `ProjectK.Common` з трьома операціями:

- `SendEmailAsync(to, subject, body)` — довільний HTML-лист;
- `SendInvitationEmailAsync(to, token)` — запрошення з посиланням `{BaseUrl}/activate/{token}`;
- `SendPasswordResetEmailAsync(to, token)` — відновлення пароля з посиланням на `/reset-password`.

## Провайдери

Провайдер обирається ключем `Email:Provider` у `appsettings.{Environment}.json`:

- **`Mock`** — значення за замовчуванням (`MockEmailService`). Нічого не надсилає, пише лист у лог
  разом із токеном — так локально можна пройти активацію без пошти.
- **`Resend`** — робочий провайдер (`ResendEmailService`) через офіційний SDK. Використовується на
  staging і в проді.

Реєстрація в DI — `ServiceCollectionExtension`, гілка за `Email:Provider`.

## Шаблон листа

Усі листи `ResendEmailService` збирає в одну рамку `Frame`: банер `assets/images/email-banner.png`
(з домену фронтенду), заголовок, текст, зелена кнопка і той самий лінк текстом для клієнтів, що
ріжуть кнопки, примітка про теку «Спам», футер. Табличний макет, усі стилі інлайном, `<meta charset>`
у голові — інакше поштові клієнти показують кракозябри. Тексти українською.

Банер рендериться з `public/assets/lileyka-banner-1080x288.svg` скриптом
`Frontend/projectk-frontend/scripts/render-email-banner.mjs` (Playwright, 1200×320).

## Налаштування (`EmailSettings`)

Секція `Email` в `appsettings`:

- `Provider` — `Mock` / `Resend`;
- `FromName`, `FromEmail` — відправник («Лілейка», адреса на верифікованому домені);
- `BaseUrl` — адреса фронтенду для посилань у листах і для банера;
- ключ Resend — у секретах середовища, не в репозиторії.

## Де використовується

- запрошення — `ProvisionMemberAccountCommandHandler` (новий учасник з акаунтом, імпорт складу),
  `ApproveWaitlistEntryCommandHandler`, `ResendInvitationCommandHandler`;
- відновлення пароля — `RequestPasswordResetCommandHandler`;
- зміна адреси акаунта — `UpdateAccountProfileCommandHandler`.

:::caution
На dev-стеку з `Provider: Resend` і недійсним ключем створення учасника з акаунтом падає на
відправці листа, хоча акаунт і запрошення вже записані. Для локальних перевірок став `Mock`.
:::
