# План патчу 0.14.0-beta

## Цілі

Патч 0.14.0-beta має закрити чотири пов'язані напрями:

- сортування колонок у таблицях мемберів;
- верифікацію актуальності профільних даних мемберів менторами, менеджерами й адміністраторами;
- стабільний дефолтний порядок проводу за роллю, а не датою каденції;
- внутрішню систему сповіщень із бекенд-сервісом та фронтенд-інбоксом.

Рекомендований підхід: реалізувати сортування і порядок проводу як малий низькоризиковий шар, потім додати kurin-level toggle для верифікації профілів, після цього додати саму верифікацію профілю як окремий доменний стан `Member`, і лише після цього підключати notification foundation та перші події. Так сповіщення зможуть одразу працювати з уже визначеними бізнес-переходами.

## Не цілі для 0.14.0-beta

- Realtime-доставка через SignalR/WebSocket.
- Персональні notification preferences.
- Архівування або видалення сповіщень користувачем.
- Повна історія усіх верифікацій профілю.
- Серверне сортування таблиць з пагінацією, якщо поточні списки залишаються невеликими.

Ці речі варто залишити для наступного патчу, щоб 0.14.0-beta не перетворився на великий інфраструктурний реліз.

## 1. Сортування колонок у таблицях мемберів

### Рішення

Для 0.14.0-beta достатньо клієнтського сортування на фронтенді через наявні PrimeNG table-механізми. Поточні екрани вже завантажують списки мемберів цілком, тому серверні `sortBy/sortDirection` не дають суттєвої користі без одночасного введення пагінації.

### Scope

- Додати `pSortableColumn` і `p-sortIcon` до колонок таблиць мемберів, де це очікувано:
  - ПІБ;
  - роль/статус;
  - пластовий рівень, якщо колонка показана;
  - група, якщо список показує весь курінь;
  - дата народження або інша профільна дата, якщо вона є в таблиці.
- Для composite-полів додати view-model sort fields, наприклад:
  - `fullNameSort`;
  - `roleSortWeight`;
  - `plastLevelSortWeight`.
- Не змінювати card view радикально. Якщо потрібна консистентність, додати компактний dropdown сортування в card view окремим follow-up.

### UX

- Користувач має бачити активну колонку сортування через стандартну іконку PrimeNG.
- Пошук і сортування повинні працювати разом: пошук фільтрує, сортування впорядковує відфільтрований список.
- Дефолтне сортування списку мемберів має залишатись стабільним і передбачуваним: ПІБ або рольовий порядок там, де список є управлінським.

## 2. Верифікація профільних даних мемберів

### Feature toggle куреня

Верифікація профільних даних не є глобальною функцією для всіх куренів. Кожен курінь має мати власний toggle, наприклад `ProfileVerificationEnabled`, який менеджер може ввімкнути або вимкнути в налаштуваннях куреня.

Рекомендована поведінка:

- за замовчуванням toggle вимкнений для існуючих і нових куренів;
- коли toggle вимкнений, UI не показує галочки верифікації й не показує дії "Верифікувати" / "Зняти верифікацію";
- backend endpoints верифікації мають повертати помилку бізнес-правила, якщо toggle вимкнений для куреня мембера;
- вимкнення toggle не повинно видаляти існуючий статус верифікації з мемберів, щоб курінь міг знову ввімкнути функцію без втрати даних;
- notification events, пов'язані саме з profile verification, створюються тільки коли toggle увімкнений.

Для керування toggle потрібна сторінка налаштувань куреня. Доступ до неї має бути тільки у менеджерів. Пункт у sidebar має вести на новий route налаштувань, а не на старий/загальний маршрут кнопки керування.

### Доменна модель

Додати до `Member` тристанний статус:

```csharp
public enum MemberProfileVerificationStatus
{
    Unverified = 0,
    VerifiedStale = 1,
    VerifiedCurrent = 2
}
```

Рекомендовані поля в `Member`:

- `ProfileVerificationStatus`;
- `ProfileVerifiedAtUtc`;
- `ProfileVerifiedByUserKey`;
- `ProfileVerificationNote`.

Для beta-версії окрему audit-таблицю можна не вводити. Поточного статусу, часу і верифікатора достатньо для UI та основної бізнес-логіки. Якщо пізніше буде потрібна повна історія, її можна додати окремою таблицею без зміни користувацького сценарію.

### Стани

1. `Unverified`: профіль не верифікований.
2. `VerifiedStale`: профіль був верифікований, але після цього дані змінились. UI: сіра галочка.
3. `VerifiedCurrent`: профіль верифікований і актуальний. UI: зелена галочка.

### Переходи

- `Unverified -> VerifiedCurrent`: ментор/менеджер/адмін підтвердив профіль.
- `VerifiedStale -> VerifiedCurrent`: відповідальна особа перевірила зміни.
- `VerifiedStale -> Unverified`: відповідальна особа зняла верифікацію.
- `VerifiedCurrent -> VerifiedStale`: будь-яка зміна істотних профільних даних.
- `VerifiedCurrent -> Unverified`: відповідальна особа вручну зняла верифікацію.

Важливо: endpoint верифікації сам по собі не повинен скидати статус у `VerifiedStale`.

### Що вважається зміною профілю

Скидати `VerifiedCurrent` у `VerifiedStale` потрібно тільки для істотних профільних даних:

- ім'я, по батькові, прізвище;
- email, телефон;
- дата народження;
- адреса;
- школа;
- група або курінь;
- пластові рівні;
- фото профілю.

Не варто скидати верифікацію через службові поля, read-state сповіщень або саму дію верифікації.

### Backend

Додати окремі команди/handlers:

- `UpdateKurinSettings(kurinKey, profileVerificationEnabled)`;
- `VerifyMemberProfile(memberKey, note?)`;
- `ResetMemberProfileVerification(memberKey)`.

API:

- `GET /api/kurin/{kurinKey}/settings`;
- `PUT /api/kurin/{kurinKey}/settings`;
- `PUT /api/member/{memberKey}/profile-verification`;
- `DELETE /api/member/{memberKey}/profile-verification`.

DTO `MemberResponse` та frontend `MemberLookupDto` мають отримати поля верифікації, щоб таблиці, картки і профіль могли показувати стан без додаткового запиту.

### Авторизація

Правило:

- тільки `Manager` має доступ до сторінки налаштувань куреня і може змінювати kurin-level toggle;
- `Admin` і `Manager` можуть верифікувати мемберів у межах куреня;
- `Mentor` може верифікувати тільки мемберів свого гуртка;
- сам мембер не може верифікувати власний профіль.

Перевірку краще робити в handler, бо там доступні `Member`, `Group`, `Kurin`, `MentorAssignments` і поточний користувач. Controller-level resource authorization має залишитись першим бар'єром, але доменний scope ментора повинен бути явним у бізнес-логіці.

### Frontend

Показ стану:

- `Unverified`: без галочки або нейтральний outline-індикатор;
- `VerifiedStale`: сіра галочка з tooltip "Дані змінено після верифікації";
- `VerifiedCurrent`: зелена галочка з tooltip "Дані верифіковано".

Дії:

- у профілі мембера або меню керування профілем показати "Верифікувати дані";
- для `VerifiedStale` показати обидві дії: "Підтвердити актуальність" і "Зняти верифікацію";
- для `VerifiedCurrent` показати "Зняти верифікацію";
- дії видимі тільки тим ролям, які мають право на конкретного мембера.

## 3. Фіксований дефолтний порядок проводу

### Рішення

Потрібен один стабільний role-order helper на фронтенді і, якщо backend повертає leadership-списки, аналогічний helper на бекенді.

Рекомендований порядок:

1. `Zvyazkovyi`;
2. `Kurinnuy`;
3. `Hurtkoviy`;
4. `Suddya`;
5. `Pysar`;
6. `Skarbnyk`;
7. `Horunjiy`;
8. `Gospodar`;
9. `Hronikar`;
10. `Instruktor`;
11. `Vykhovnyk`.

### Сортування

Для leadership-списків:

1. активна каденція перед завершеною;
2. role-order weight;
3. ПІБ мембера або назва ролі як tie-breaker;
4. `StartDate` descending тільки як останній tie-breaker.

Це замінює поточну поведінку, де порядок залежить переважно від дати каденції.

### UX

- Дефолтний порядок повинен бути однаковим у таблицях, картках ролей і тегах ролей біля мемберів.
- Якщо користувач вручну сортує колонку, його вибір має тимчасово перебивати дефолтний порядок.

## 4. Внутрішня система сповіщень

### Рішення

Для 0.14.0-beta зробити persistent in-app notifications без realtime. Це дає inbox, unread-count і audit-friendly історію подій без складності SignalR. Realtime можна додати пізніше поверх тієї ж моделі.

### Backend-модель

Нова сутність, наприклад `AppNotification`:

- `NotificationKey`;
- `RecipientUserKey`;
- `Type`;
- `Severity`;
- `Title`;
- `Body`;
- `EntityType`;
- `EntityKey`;
- `Route`;
- `PayloadJson`;
- `CreatedAtUtc`;
- `ReadAtUtc`;
- `ActorUserKey`;
- `DeduplicationKey`;
- `ExpiresAtUtc`.

`PayloadJson` має бути невеликим і не повинен ставати джерелом правди. Основні переходи мають жити в доменних сутностях, а payload потрібен тільки для UI-контексту.

### Типи подій для beta

Перший набір:

- `MemberProfileVerified`;
- `MemberProfileChangedAfterVerification`;
- `MemberSkillSubmittedForReview`;
- `MemberAwardSubmitted`;
- `MemberAwardReviewed`;
- `MemberWarningAssigned`;
- `LeadershipChanged`.

Це покриває найбільш корисні активні дії без спроби одразу описати всі можливі події системи.

### Backend-сервіс

Додати сервіс/адаптер, наприклад `INotificationService`:

```csharp
public interface INotificationService
{
    Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken);
    Task NotifyManyAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(NotificationQuery query, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid userKey, CancellationToken cancellationToken);
    Task MarkAsReadAsync(Guid notificationKey, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(Guid userKey, CancellationToken cancellationToken);
}
```

Рекомендація: у beta handlers можуть викликати notification service напряму після успішного збереження доменної зміни. Не використовувати fire-and-forget. Якщо пізніше з'являться domain events/outbox, notification service можна підключити до них без зміни frontend API.

### API

Додати `NotificationController`:

- `GET /api/notifications?unreadOnly=false&take=50`;
- `GET /api/notifications/unread-count`;
- `PUT /api/notifications/{notificationKey}/read`;
- `PUT /api/notifications/read-all`.

Всі endpoints працюють тільки для поточного користувача. Користувач не має передавати `recipientUserKey` у read-запитах.

### Frontend

Компоненти:

- `NotificationService`;
- `NotificationInboxPage` або drawer;
- bell у верхній навігації з unread badge;
- item-компонент для notification row.

Поведінка:

- unread badge оновлюється після login, після відкриття inbox і після mark-read;
- клік по notification переводить на `route` і позначає її прочитаною;
- є дія "Позначити всі як прочитані";
- inbox групує записи "Сьогодні", "Раніше" або хоча б сортує newest first.

### Антиспам і deduplication

Для подій, які можуть часто повторюватись, використовувати `DeduplicationKey`, наприклад:

- `profile-stale:{memberKey}`;
- `skill-review:{memberKey}:{skillKey}`;
- `award-review:{awardKey}`.

У beta достатньо не створювати дубльоване unread-сповіщення з тим самим ключем. Якщо існуюче вже прочитане, можна створити нове або оновити `CreatedAtUtc`, залежно від події.

## Ризики

- Хибне скидання верифікації: якщо `UpsertMember` скидатиме статус на будь-яку зміну, користувачі втратять довіру до галочки.
- Недостатній mentor scope: ментор не повинен верифікувати мемберів іншого гуртка.
- Notification spam: без deduplication inbox швидко стане шумним.
- Приватність: notification payload не має відкривати персональні дані тим, хто не має права бачити відповідну сутність.
- UI density: додаткові іконки в таблицях мемберів можуть погіршити мобільний вигляд.
- Міграція: всі існуючі мембери мають стартувати як `Unverified`, якщо немає окремого backfill-рішення.
- Консистентність read-state: mark-read має бути idempotent і працювати тільки для поточного користувача.

## UI/UX питання

- Де саме показувати дію верифікації: у header профілю, меню картки чи налаштуваннях мембера? Рекомендація: показувати статус у header/таблиці, а дії тримати в меню керування профілем.
- Як назвати `VerifiedStale` українською? Рекомендація: "Верифіковано, але дані змінено".
- Чи показувати `Unverified` як явний бейдж? Рекомендація: у таблицях не шуміти, а в профілі показувати нейтральний стан.
- Чи потрібне сортування card view? Рекомендація: не блокувати beta, але додати пізніше, якщо користувачі активно працюють у card view.
- Чи потрібне видалення сповіщень? Рекомендація: ні для beta; тільки read/unread.

## Послідовність робіт

1. Додати сторінку налаштувань куреня, manager-only route і sidebar link.
2. Додати backend поле/settings endpoint для `ProfileVerificationEnabled`.
3. Додати fixed leadership role order на фронтенді й оновити сортування проводу.
4. Розширити таблиці мемберів sortable columns і view-model sort fields.
5. Додати backend enum/fields/migration для profile verification.
6. Додати verification commands, handlers, API endpoints і authorization checks.
7. Протягнути verification fields у DTO/frontend models.
8. Додати UI-індикатори й actions верифікації, які залежать від kurin toggle.
9. Додати notification entity, repository, service, controller.
10. Додати frontend notification service, bell badge й inbox.
11. Підключити перші notification events: profile verified/stale, skill submitted for review, award reviewed, warning assigned.
12. Додати tests і e2e coverage для ключових сценаріїв.

## Acceptance criteria

- Колонки таблиць мемберів сортуються стабільно і не ламають пошук.
- Провід за замовчуванням показується у фіксованому role-order незалежно від дати каденції.
- Менеджер бачить сторінку налаштувань куреня в sidebar і може вмикати/вимикати profile verification.
- Не-менеджер не бачить сторінку налаштувань куреня і не може змінити toggle через API.
- Коли toggle вимкнений, UI не показує verification controls, а backend verification endpoints блокують дію.
- Новий мембер або існуючий після міграції має `Unverified`.
- Верифікація переводить профіль у `VerifiedCurrent`.
- Істотний update профілю переводить `VerifiedCurrent` у `VerifiedStale`.
- Ментор не може верифікувати мембера з чужого гуртка.
- Менеджер/адмін може верифікувати мемберів свого куреня.
- Після верифікації мембер отримує notification.
- Після зміни вже верифікованого профілю відповідальні користувачі отримують notification.
- Inbox показує unread/read, відкриває route і підтримує mark-read/mark-all-read.

## Рекомендований scope для 0.14.0-beta

Включити:

- sortable member tables;
- fixed leadership order;
- profile verification з трьома станами;
- notification foundation, inbox, unread count;
- перші high-value notification events.

Відкласти:

- realtime delivery;
- notification preferences;
- bulk verification;
- повний audit trail верифікації;
- notification archive/delete;
- server-side sorting/pagination.

Цей scope дає користувачам видимий результат у кожному з чотирьох напрямів, але не змушує патч одночасно переробляти інфраструктуру списків, realtime і audit-підсистему.
