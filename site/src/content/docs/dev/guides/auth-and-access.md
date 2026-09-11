---
title: Автентифікація й доступ
description: Вхід, сесії, MFA, онбординг через список очікування і як перевіряються права.
sidebar:
  order: 8
---

Огляд для розробника. Модель прав (уряди → ролі → дозволи) описана в
[Архітектурі](/dev/core/architecture/), тут — маршрути й механіка.

## Вхід і сесії

`AuthController` (`AuthModule`), JWT Bearer з refresh-сесіями.

- `POST /register/kurin` — адмін створює курінь разом з першим акаунтом Звʼязкового;
  `POST /register` — акаунт для людини, яка вже є в курені.
- `POST /login` — вхід. Якщо в акаунта є другий фактор і пристрій ще не довірений, відповідь несе
  `requiresMfa` і короткоживучий `mfaToken` замість токенів.
- `POST /mfa/login-verify` — другий крок: код з автентифікатора або резервний код разом з
  `mfaToken`. Успіх ставить cookie `mfaTrust` (`Security:MfaTrustDays`, 7 днів), і наступні входи з
  цього пристрою минають другий крок, поки не зміниться security stamp акаунта.
- `POST /refresh` · `POST /logout` — оновлення й завершення сесії. Сесія — рядок у
  `UserRefreshTokens`; вхід додає рядок, вихід завершує один, зміна пароля чи MFA завершує всі.
  Вихід не чіпає `mfaTrust`.
- `POST /kurin-scope` — вибір активного куреня; токен доступу несе ролі саме для нього.
- `POST /check-access` — серверна перевірка доступу до конкретного ресурсу, якою користується
  фронтенд, щоб ховати кнопки за тим самим рішенням, що й ендпоінт.

## Онбординг: список очікування → запрошення → активація

`OnboardingController`. Потік: заявка → модерація → запрошення → активація.

- `POST /waitlist` — заявка (зараз лише від Звʼязкового куреня); `GET /waitlist` — список для
  адміна;
- `POST /waitlist/{key}/approve` — схвалити: створює курінь і акаунт, надсилає запрошення;
  `POST /waitlist/{key}/reject` — відхилити; `POST /waitlist/{key}/resend-invitation` — надіслати
  ще раз;
- `GET /invitation/{token}/validate` — перевірити токен;
- `POST /activate` — задати пароль. Відповідь — `LoginUserResponse` з refresh-cookie: людина
  одразу в застосунку;
- `POST /password-reset/request` · `POST /password-reset/reset` — відновлення пароля. Запит також
  надсилає нове запрошення, якщо акаунт ще не активований.

Листи йдуть через `IEmailService` — див. [Пошта](/dev/guides/email/). Сутності: `WaitlistEntry`,
`Invitation`.

## MFA

TOTP. `GET /mfa/setup` (секрет і QR) → `POST /mfa/enable` (код) → `POST /mfa/recovery-codes`
(резервні коди, за поточним паролем). `GET /mfa/status` каже, чи увімкнено і чи вимагається.

**Примус для проводу:** `PrivilegedMfaEnforcementMiddleware` + `MfaEnforcementPolicy` вимагають
другий фактор для адміністратора і Звʼязкового на `Production` і `Staging`; `Security:EnforcePrivilegedMFA`
керує цим на self-host. Ключ `E2E:BypassPrivilegedMfa` читається лише на тестових тирах. Фронтенд:
`mfa-setup-dialog` (обовʼязковий діалог після входу), `account-settings`.

## Дозволи на бекенді

- `[Authorize(Policy = …)]` — грубі політики (`RequireUser`, `RequireAdmin`, `RequireKurinManagement`…).
- `[ResourceAuthorize(ResourceType, ResourceAction, "route:key")]` — дозвіл на конкретний ресурс:
  `RolePermissionMap` перекладає ролі з урядів у дозволи зі скоупом (`KurinWide`, `OwnGroups`, `Self`),
  а `AccessContextResolver` визначає, в якому курені людина стоїть і які уряди тримає.
- Активне закріплення впорядника за гуртком (`MentorAssignment`) трактується як уряд впорядника в КВ.
- Обидві перевірки покриті тестом `AuthorizationBaselineMatrixTests`: кожна дія контролера має
  бути в матриці, інакше тест падає.

## Guard-и (Angular)

- `authGuard` — вимагає автентифікації;
- `kurinAccessGuard('kurin' | 'panel')` — активний курінь у скоупі, або адмін-панель;
- `capabilityGuard('admin')` — можливість за роллю;
- `EntityGuard` — доступ до конкретної сутності через `check-access`, **fail-closed**: помилка
  сервера означає «заборонено»;
- `setupGuard` — майстер первинного налаштування;
- `publicAuthRedirectGuard` — вже автентифікованих з публічних сторінок веде додому.

Принцип: guard-и ховають, бекенд вирішує. Жодна перевірка на фронті не є останньою.

## Локальні дев-інструменти

На `Development`, `E2E`, `Tailscale` є `DevToolsController` (`api/dev/impersonate`,
`api/dev/impersonate/member`, `api/dev/return`) і перемикач ролей на правому краю екрана: адмін
заходить за уряд у курені або як людина з відкритої картки і повертається квитком. На інших тирах
контролер знімається з моделі (`DevOnlyControllerFeatureProvider`).
