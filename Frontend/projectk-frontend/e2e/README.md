# ProjectK UI E2E Tests

Playwright is the UI automation layer for Stage 8 of `0.13.0-beta`.

## Current Scope

- Login through the real `/login` UI for seeded users.
- Persist authenticated browser state per role under `e2e/.auth`.
- Use `describeRole(role, title, tests)` from `e2e/support/role-test.ts` for authenticated role suites. It applies the correct `storageState` and refresh-token interception in one place.
- Use scenario builders from `e2e/support/scenarios.ts` for repeatable dynamic data setup such as activated member accounts and group-with-mentor test state.
- Smoke public pages: welcome, join, login.
- Check basic role access:
  - admin can open admin-only pages;
  - manager can open kurin panel;
  - member cannot open admin-only pages.
- Check the first authenticated mutation flow:
  - manager opens seeded `Gurtok 1`;
  - manager updates the group description through the `Редагувати` menu.

- Check role-sensitive UI controls:
  - manager-only mutate buttons and member form validation states;
  - warning checkboxes that unlock by warning level;
  - mentor review access without member edit controls;
  - member restriction from skill moderation controls.
- Check role/action route boundaries:
  - anonymous users redirect from protected organization routes;
  - member create/edit routes use backend `Create`/`Update` access checks instead of read-only checks;
  - mentors can create members only in assigned groups;
  - leadership setup routes are manager/admin-only;
  - member self-edit hides destructive and warning controls.
- Check conditional organization UI:
  - kurin-level profile, group, KV, leadership and member controls by role;
  - group action menu contents by role and assigned mentor scope;
  - planning create/delete controls by role.
- Check «Архів куреня» (`kurin-archive.spec.ts`), each test in a kurin of its own from `e2e/support/kurin-archive.ts`:
  - the Звʼязковий sees former roles and memberships and corrects a closed membership's dates;
  - their own record carries «своє» and no correction;
  - a гуртковий sees only their гурток's former memberships and no danger zone;
  - «Розформувати» sends an empty гурток to the archive and refuses one with money in its box, with a message.
- Check the archive's danger zone (`kurin-archive-danger.spec.ts`):
  - a wrong password is refused, the right one opens the zone with ten deletions;
  - one deletion at a time, after a confirmation;
  - a membership with dues and one whose role is still held are refused, each with its message;
  - leaving the page ends the zone on the server: its ticket no longer deletes.
- Check «Пластовий життєпис» (`life-story.spec.ts`):
  - a youth's story has only the УПЮ stage (both projects);
  - for the senior from the demo seed: the button on the card, the УПЮ and УСП stages with every ступінь, and the Order's Звʼязковий may correct them while its Скриптор may not (chromium only: it signs in as shared seeded accounts).

## Seeded Users

Default credentials come from the backend development `DataSeeder`:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@projectk.com` | `Admin@12345` |
| Manager | `manager1@projectk.com` | `User@12345` |
| Mentor | `mentor1@projectk.com` | `User@12345` |
| Member | `g1member1@projectk.com` | `User@12345` |

Override with environment variables when needed:

- `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`
- `E2E_MANAGER_EMAIL`, `E2E_MANAGER_PASSWORD`
- `E2E_MENTOR_EMAIL`, `E2E_MENTOR_PASSWORD`
- `E2E_MEMBER_EMAIL`, `E2E_MEMBER_PASSWORD`
- `E2E_DEFAULT_PASSWORD`

## Local Run

Start backend on the URL used by `src/environments/environment.ts`:

```powershell
dotnet run --project Backend\ProjectK.Backend\ProjectK.API\ProjectK.API.csproj
```

Then run from `Frontend/projectk-frontend`:

```powershell
npm run e2e
```

For isolated E2E reset, run backend with the E2E environment:

```powershell
$env:ASPNETCORE_ENVIRONMENT='E2E'
dotnet run --project Backend\ProjectK.Backend\ProjectK.API\ProjectK.API.csproj
```

Then enable reset in Playwright:

```powershell
$env:E2E_RESET_ENABLED='true'
$env:E2E_RESET_TOKEN='local-e2e-reset-token'
$env:PLAYWRIGHT_API_URL='http://127.0.0.1:5205/api'
npm run e2e
```

By default Playwright starts Angular on `http://127.0.0.1:4200`.
If Angular is already running:

```powershell
$env:PLAYWRIGHT_START_FRONTEND='false'
npm run e2e
```

Override frontend base URL:

```powershell
$env:PLAYWRIGHT_BASE_URL='http://127.0.0.1:4200'
npm run e2e
```

## Backend Reset Endpoint

The stable Stage 8 flow uses:

- `ASPNETCORE_ENVIRONMENT=E2E`;
- `appsettings.E2E.json` with a separate DB, blob container/prefix and disabled orphan cleanup;
- a reset endpoint such as `POST /api/test/e2e/reset`, available only in `E2E` and protected by a local secret header.

That reset endpoint runs before auth setup when `E2E_RESET_ENABLED=true`, so CRUD tests can create/update/delete entities without leaking state across runs.
