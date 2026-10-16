# Статус Лілейки

Окремий Cloudflare Worker на безкоштовному плані: публічна сторінка стану сервісів і анонімна телеметрія від інстансів Лілейки (селф-хост бандли й прод).

- `/` показує, чи відповідають API, вебзастосунок, каталог і документація: стан зараз, смужки за 90 днів і середній час відповіді за 24 години.
- `/admin` (за Cloudflare Access) показує живі інстанси, версії, краї, країни й навантаження, а також дає писати оголошення на сторінку статусу.
- Крон кожні 2 хвилини перевіряє `TARGETS`. Невдалу перевірку він повторює через 2 с, а при падінні чи відновленні пише в Telegram, якщо задано бота.

## API

| Метод | Шлях | Що робить |
| --- | --- | --- |
| GET | `/v1/status` | стан для сторінки, кеш на краю 60 с |
| POST | `/v1/ping` | пінг інстансу, відповідає `{ ok, latest }` |
| GET | `/v1/admin/telemetry` | підсумки для адмінки |
| GET, POST, PATCH, DELETE | `/v1/admin/incidents` | оголошення: `incident`, `maintenance`, `notice` |
| DELETE | `/v1/admin/instances/:id` | прибрати сміттєвий інстанс |
| POST | `/v1/admin/check` | перевірити цілі зараз |

Пінг (до 2 КБ) містить лише числа й коди:

```json
{
  "instanceId": "3f2a9c1e-5b7d-4e8a-9c21-7d4e5f6a8b90",
  "version": "v1.2.0",
  "kind": "selfhost",
  "krais": ["cz"],
  "kurins": 2, "members": 130,
  "activeUsers7d": 12, "activeUsers30d": 25,
  "requests24h": 4200, "errors24h": 3,
  "uptimeHours": 71
}
```

IP не зберігається, лише країна, яку визначає Cloudflare. Нових інстансів за добу приймається не більше 200.

## Ліміти Free

D1 дає 100 тис. записаних рядків на добу на весь акаунт, і цей ліміт спільний із каталогом. Тому перевірки пишуться не окремими рядками, а як лічильники години й дня, тобто приблизно 3 рядки на перевірку: близько 9 тис. на добу для 4 цілей. Щоденне прибирання о 03:00 UTC видаляє погодинні дані старші за 2 доби, денні старші за 400 днів і події старші за 90 днів.

## Перший деплой

```sh
cd status
npm ci
npx wrangler d1 create status   # id вписати в wrangler.jsonc → d1_databases[0].database_id
npm run db:migrate:remote
npx wrangler deploy             # або Workers Builds: root directory status, watch path status/*
```

- **Access.** Додати до застосунку Access шляхи `status.rostyslav-mukha.dev/admin` і `status.rostyslav-mukha.dev/v1/admin`, а його AUD вписати в `ACCESS_AUD`.
- **Telegram (необов'язково).** Виконати `npx wrangler secret put TELEGRAM_BOT_TOKEN` і вписати `TELEGRAM_CHAT_ID` у `vars`.
- **Переїзд на новий домен.** Змінити `routes` і `TARGETS`. Старий сабдомен краще лишити другим маршрутом, поки селф-хости не оновляться.

## Локально

```sh
printf 'ENVIRONMENT=dev\nDEV_ACCESS_EMAIL=you@example.com\n' > .dev.vars
npm run db:migrate:local
npx wrangler dev --test-scheduled --local-upstream localhost:8787   # крон: curl localhost:8787/__scheduled
npm test
```
