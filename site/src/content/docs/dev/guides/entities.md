---
title: Каталог сутностей
description: Доменні сутності за модулями — що є в ProjectK.Common/Entities і навіщо.
sidebar:
  order: 3
---

Перелік доменних сутностей (`ProjectK.Common/Entities`), згрупований за модулями. Спільний базовий
клас — `Entity`. Межі модулів і те, хто в кого може питати, описано в
[Архітектурі](/dev/core/architecture/).

## Auth

- **AppUser** — обліковий запис (ASP.NET Identity), має `OnboardingStatus` (активний, запрошений,
  призупинений). `ActiveKurinKey` — курінь, у якому людина зараз діє.
- **AppRole** — системна роль (Identity). Насправді значуща лише `Admin`; уряди в курені дають
  ролі вигляду `KV.Zvyazkovyi` під час входу, а не зберігаються в Identity.
- **UserRefreshToken** — одна сесія браузера. Вхід додає рядок, вихід завершує один, зміна пароля
  чи MFA завершує всі.
- **Invitation** — запрошення з токеном активації.
- **WaitlistEntry** — заявка зі списку очікування (`WaitlistVerificationStatus`).
- **UserTileLayout** — збережене розташування плиток на панелі користувача.

## Kurin (ядро домену)

- **Kurin** — курінь (за числом).
- **Group** — гурток у курені.
- **Member** — людина. Від 0.20.0 член не належить куреню напряму.
- **Membership** — належність людини до куреня (з гуртком, датами приходу й виходу). Одна людина
  може стояти в кількох куренях; активний вибирається при вході.
- **Leadership** / **LeadershipHistory** — провід (КВ, курінний, гуртковий) і хто коли який уряд
  тримав. Саме з активних записів виводяться права.
- **MentorAssignment** — закріплення впорядника за гуртком. Активне закріплення трактується як
  уряд впорядника в КВ.
- **MemberAward** — відзначення; **MemberWarning** — зауваження й стягнення.
- **PlastLevelHistory** — історія пластових ступенів.
- **PlanningSession**, **PlanningParticipant**, **ParticipantBusyRange** — планування заходів і
  зайнятість учасників.
- **AgendaItem**, **AgendaItemAssignment**, **AgendaEventGroup** — календар і задачі: події й задачі
  з призначенням «для кого», повторами й відгуками.

## Probes & Badges

- **ProbeProgress** / **ProbePointProgress** — проба і її окремі точки.
- **BadgeProgress** — вмілість.
- **ProbeProgressAuditEvent** / **BadgeProgressAuditEvent** — журнал змін поступу.

Каталог самих проб і вмілостей у базі не живе — його віддає пакет `ProjectK.ProbeAndBadges`.

## Infrastructure

- **AppNotification** — сповіщення користувача.
- **PublicAnnouncementDraft** — чернетка публічного анонсу.
- **SystemSetting** — системні налаштування (ключ-значення).
- **ActivityLog** / аудит — записи безпекових подій (вхід, MFA, дії адміністратора, дев-перемикач).

:::tip
Числа enum, що зберігаються в базі, заморожені: додавати можна лише в кінець. Правило й причина —
у [Конвенціях](/dev/core/contributing/).
:::
