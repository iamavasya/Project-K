---
title: 'Спостережуваність у проді'
description: 'Логи, метрики, здоровʼя сервісу в проді.'
sidebar:
  order: 1
---

:::note[Джерело]
Ця сторінка збирається з [`docs/observability.md`](https://github.com/iamavasya/Project-K/blob/main/docs/observability.md) у репозиторії. Правити треба там.
:::
API надсилає телеметрію ASP.NET Core в ресурс Application Insights, заданий змінною
`APPLICATIONINSIGHTS_CONNECTION_STRING`. Той самий ресурс отримує трейси Serilog через
`Serilog__WriteTo__1__Args__connectionString`.

Обидва значення в проді мають бути посиланнями на Key Vault в App Service. Рядок підключення чи
instrumentation key не потрапляє ні в репозиторій, ні в запити KQL.

## Перші перевірки після деплою

Зроби запит на `/health`, зачекай хвилину на інжест і виконай:

```kusto
requests
| where timestamp > ago(30m)
| summarize Requests=count(), Failed=countif(success == false), P95=percentile(duration, 95)
```

Окрім `traces`, API має давати `requests` і `dependencies`. Необроблена помилка запиту зʼявляється в
`exceptions`.

## Корисні запити

### Невдалі запити

```kusto
requests
| where timestamp > ago(24h)
| where success == false or toint(resultCode) >= 400
| project timestamp, resultCode, duration, name, url, operation_Id
| order by timestamp desc
```

### Винятки

```kusto
exceptions
| where timestamp > ago(24h)
| project timestamp, type, outerMessage, problemId, operation_Id
| order by timestamp desc
```

### Відновлення пароля і запрошення

```kusto
union isfuzzy=true
(
    requests
    | where timestamp > ago(7d)
    | where url has_any ("password-reset", "invitation/resend", "activate")
    | project timestamp, itemType="request", name, resultCode, success, operation_Id
),
(
    traces
    | where timestamp > ago(7d)
    | where message has_any ("PasswordReset", "Invitation", "RequestPasswordReset")
    | project timestamp, itemType="trace", name="", resultCode="", success="", operation_Id, message
)
| order by timestamp desc
```

### Аудит безпеки

Безпекові події (`Auth.*`, `Mfa.*`, `Dev.Impersonate`, призупинення акаунтів) пишуться через
`IActivityLogger` як структуровані трейси:

```kusto
traces
| where timestamp > ago(7d)
| where customDimensions.Action startswith "Auth." or customDimensions.Action startswith "Mfa."
| project timestamp, customDimensions.Action, customDimensions.ActorUserId, customDimensions.TargetUserId, message
| order by timestamp desc
```

### Один запит цілком

Візьми `operation_Id` із запиту, трейсу чи винятку і подивись увесь ланцюжок:

```kusto
union isfuzzy=true requests, dependencies, exceptions, traces
| where operation_Id == "<operation-id>"
| project timestamp, itemType, name, target, resultCode, success, message, type
| order by timestamp asc
```

## Доступ

У ресурсу Application Insights має бути увімкнений мережевий доступ до запитів. Людині, що
дивиться логи, потрібна щонайменше роль `Monitoring Reader` на ресурсі або його групі. Керована
ідентичність API до цього не стосується: їй потрібен лише доступ до Key Vault для посилань на
рядок підключення.

## Health-ендпоінт

`GET /health` відповідає без автентифікації; фронтенд опитує його і показує банер «сервер
прокидається», поки App Service холодний. Той самий ендпоінт використовує healthcheck контейнера в
self-host.
