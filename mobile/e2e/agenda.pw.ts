import { expect, test, type Page } from '@playwright/test';
import { member, ok, refuse, shot, signIn, soon, type ExtraApi } from './support/mock-api';

/**
 * The agenda: calendar, event and task details, the board and the forms. Fixtures are shaped like
 * the API's AgendaItemResponse; `leader` flips the per-item flags the API would send to провід.
 */

const localDay = (iso: string) => {
  const date = new Date(iso);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
};
const allDay = (iso: string) => `${localDay(iso)}T00:00:00Z`;

const sokoly = { agendaAssignmentKey: 'a1', targetType: 'Group', targetKey: 'g1', label: 'Соколи', completionMode: 'Shared',
  status: 'Todo', statusChangedByName: null, statusChangedAtUtc: null, canChangeStatus: false, doneCount: null, peopleCount: null, parts: null };

function item(overrides: Record<string, unknown>) {
  return {
    kurinKey: 'k1', kind: 'Event', description: null, location: null, status: 'Todo', viewerStatus: 'Todo', startUtc: null,
    endUtc: null, isAllDay: false, createdByUserKey: 'u1', createdByName: 'Богдан Гончар', createdUtc: soon(-10, 9),
    updatedUtc: soon(-1, 9), completedAtUtc: null, archivedAtUtc: null, archivedByName: null, canEdit: false,
    canChangeStatus: false, addressedToViewer: true, categoryKey: null, categoryName: null, categoryColorHex: null,
    categoryIcon: null, isKurinSchedule: false, audience: 'Assigned', recurrenceFrequency: 'None', recurrenceInterval: 1,
    recurrenceByWeekday: 0, recurrenceEndUtc: null, recurrenceCount: null, isRecurrenceInstance: false,
    seriesStartUtc: null, seriesEndUtc: null, assignments: [sokoly],
    ...overrides,
  };
}

interface Fixtures {
  extra: ExtraApi;
  reads: { path: string; query: URLSearchParams }[];
}

/** The agenda endpoints as the API answers a youth, or (`leader`) the провід who may edit. */
function agendaApi(leader = false): Fixtures {
  const reads: Fixtures['reads'] = [];
  const meeting = soon(1, 17);
  const series = item({
    agendaItemKey: 'e1', title: 'Сходини гуртка', startUtc: meeting, endUtc: soon(1, 19), location: 'Пласт-дім, Львів',
    description: 'Вузли і карта. Принеси зошит.', categoryKey: 'c1', categoryName: 'Сходини', categoryColorHex: '#2e7d32',
    recurrenceFrequency: 'Weekly', recurrenceByWeekday: 1 << new Date(meeting).getDay(), isRecurrenceInstance: true,
    seriesStartUtc: soon(-6, 17), seriesEndUtc: soon(-6, 19), canEdit: leader,
  });
  const hike = item({
    agendaItemKey: 'e2', title: 'Мандрівка на Говерлу', startUtc: allDay(soon(5, 8)), endUtc: allDay(soon(6, 8)), isAllDay: true,
    location: 'Заросляк', canEdit: leader, assignments: [{ ...sokoly, targetType: 'Kurin', targetKey: 'k1', label: 'Курінь 7' }],
  });
  const orly = item({
    agendaItemKey: 'e3', title: 'Сходини', startUtc: soon(2, 18), endUtc: soon(2, 20), isKurinSchedule: true, audience: 'Schedule',
    assignments: [{ ...sokoly, targetKey: 'g2', label: 'Орли' }],
  });
  const knots = item({
    agendaItemKey: 't1', kind: 'Task', title: 'Вивчити вузли', startUtc: allDay(soon(0, 8)), endUtc: allDay(soon(3, 8)), isAllDay: true,
    viewerStatus: 'InProgress', status: 'InProgress', canChangeStatus: true, canEdit: leader,
    assignments: [{
      ...sokoly, completionMode: 'PerMember', status: 'InProgress', doneCount: 1, peopleCount: 2,
      parts: leader ? [
        { memberKey: 'm1', name: 'Остап Коваль', status: 'Done', changedByName: 'Остап Коваль', changedAtUtc: soon(-1, 12), canChangeStatus: true },
        { memberKey: 'm2', name: 'Марко Шевчук', status: 'Todo', changedByName: null, changedAtUtc: null, canChangeStatus: true },
      ] : null,
    }],
  });
  const tent = item({ agendaItemKey: 't2', kind: 'Task', title: 'Принести намет', viewerStatus: 'InProgress', status: 'InProgress',
    endUtc: soon(-1, 20), canChangeStatus: true, canEdit: leader });
  const done = item({ agendaItemKey: 't3', kind: 'Task', title: 'Зібрати внески', viewerStatus: 'Done', status: 'Done', canEdit: leader,
    assignments: [{ ...sokoly, status: 'Done', statusChangedByName: 'Богдан Гончар', statusChangedAtUtc: soon(-2, 10) }] });
  const todo = [
    { ...knots, viewerStatus: 'Todo' },
    ...Array.from({ length: 20 }, (_, i) => item({ agendaItemKey: `tt${i}`, kind: 'Task', title: `Задача ${i + 1}`, canEdit: leader, canChangeStatus: true })),
  ];
  const columns = { Todo: todo, InProgress: [tent], Done: [done] } as Record<string, any[]>;
  const calendar = [series, orly, hike, knots];
  let myStatus: string | null = null;

  const picture = () => ({
    agendaItemKey: 'e1', occurrenceStartUtc: meeting, capacity: null, waitlistEnabled: false, myStatus,
    goingConfirmedCount: 5 + (myStatus === 'Going' ? 1 : 0), goingWaitlistCount: 0, notGoingCount: 1, maybeCount: 2,
    responses: [
      { userKey: 'u2', displayName: 'Марко Шевчук', status: 'Going', respondedAtUtc: soon(-1, 9), isWaitlisted: false },
      { userKey: 'u3', displayName: 'Ірина Бойко', status: 'Maybe', respondedAtUtc: soon(-1, 10), isWaitlisted: false },
    ],
  });

  const extra: ExtraApi = ({ method, path, query, body }) => {
    if (method === 'GET') reads.push({ path, query });
    if (method === 'GET' && path === 'agenda/k1') {
      const from = new Date(query.get('fromUtc')!).getTime();
      const to = new Date(query.get('toUtc')!).getTime();
      const schedules = query.get('includeSchedules') === 'true';
      return ok(calendar.filter((i) => {
        const start = new Date(i.startUtc as string).getTime();
        const end = new Date((i.endUtc ?? i.startUtc) as string).getTime();
        return start <= to && end >= from && (schedules || i.audience !== 'Schedule');
      }));
    }
    if (method === 'GET' && path === 'agenda/k1/board') {
      const search = (query.get('search') ?? '').toLowerCase();
      const mine = query.get('onlyMine') === 'true';
      const pick = (list: any[]) => list.filter((t) => t.title.toLowerCase().includes(search) && (!mine || t.agendaItemKey === 't1'));
      const status = query.get('status');
      const skip = Number(query.get('skip') ?? 0);
      const take = Number(query.get('take') ?? 20);
      const targets = [{ targetType: 'Group', targetKey: 'g1', label: 'Соколи' }, { targetType: 'Member', targetKey: 'm1', label: 'Остап Коваль' }];
      const statuses = status ? [status] : ['Todo', 'InProgress', 'Done'];
      return ok({
        columns: statuses.map((s) => ({ status: s, total: pick(columns[s]).length, items: pick(columns[s]).slice(skip, skip + take) })),
        targets,
      });
    }
    if (method === 'GET' && path.startsWith('agenda/item/')) {
      const key = path.split('/')[2];
      const found = [series, hike, knots, tent, done].find((i) => i.agendaItemKey === key);
      if (!found) return refuse(404, 'AGENDA_NOT_FOUND');
      // The item endpoint gives the series itself, not an occurrence.
      return ok(key === 'e1' ? { ...found, startUtc: found.seriesStartUtc, endUtc: found.seriesEndUtc, isRecurrenceInstance: false } : found);
    }
    if (method === 'GET' && path === 'agenda/e1/responses') return ok(picture());
    if (method === 'GET' && path.endsWith('/responses')) return ok({ ...picture(), responses: [], goingConfirmedCount: 0, maybeCount: 0, notGoingCount: 0 });
    if (method === 'PUT' && path === 'agenda/e1/response') {
      myStatus = body.status;
      return ok(picture());
    }
    if (method === 'GET' && path === 'agenda/k1/assign-targets') {
      return ok({
        canTargetKurin: true, kurinKey: 'k1', kurinLabel: 'Курінь 7',
        kurinLeaderships: [{ leadershipKey: 'l0', label: 'Курінний провід', canTarget: true }],
        groups: [
          { groupKey: 'g1', name: 'Соколи', canTargetGroup: true, leadership: { leadershipKey: 'l1', label: 'Провід Соколів', canTarget: true },
            members: [{ memberKey: 'm1', fullName: 'Остап Коваль' }, { memberKey: 'm2', fullName: 'Марко Шевчук' }] },
          { groupKey: 'g2', name: 'Орли', canTargetGroup: true, leadership: null, members: [{ memberKey: 'm3', fullName: 'Ірина Бойко' }] },
        ],
      });
    }
    if (method === 'GET' && path === 'agenda/k1/categories') {
      return ok([{ agendaCategoryKey: 'c1', kurinKey: 'k1', name: 'Сходини', colorHex: '#2e7d32', icon: null, capacity: null,
        waitlistEnabled: false, defaultDescription: 'Принеси зошит.', rsvpRequired: true, defaultDurationMinutes: 120,
        reminderLeadMinutes: null, isArchived: false, isKurinSchedule: true }]);
    }
    if (method === 'POST' && path === 'agenda') return ok('new-item');
    return null;
  };
  return { extra, reads };
}

/** Провід: the youth's sign-in with the agenda and scoring grants added, as the web reads them. */
async function signInAsLeader(page: Page, extra: ExtraApi, path: string) {
  const log = await signIn(page, member, extra);
  await page.evaluate(() => {
    const state = JSON.parse(localStorage.getItem('authState')!);
    state.permissions = ['AgendaItem:Create:OwnGroups', 'GroupScore:Create:OwnGroups'];
    localStorage.setItem('authState', JSON.stringify(state));
  });
  await page.goto(path);
  return log;
}

async function openCalendar(page: Page) {
  await page.locator('ion-tab-button[tab=calendar]').click();
  await expect(page.locator('app-calendar [data-testid=month]')).toBeVisible();
}

/** The day's cell; a day past the grid's last week is one month on. */
async function pickDay(page: Page, iso: string) {
  await expect(page.locator('app-calendar [data-testid=month]')).toBeVisible();
  const cell = page.locator(`app-calendar [data-day="${localDay(iso)}"]`);
  if (!(await cell.count())) await page.locator('app-calendar').getByRole('button', { name: 'Наступний місяць' }).click();
  await cell.click();
}

test('shows the month with its dots, the chosen day and the coming list', async ({ page }, info) => {
  const { extra, reads } = agendaApi();
  await signIn(page, member, extra);
  await openCalendar(page);
  const calendar = page.locator('app-calendar');
  await expect(calendar.getByRole('button', { name: 'Нова подія' })).toHaveCount(0);

  await pickDay(page, soon(1, 17));
  const agenda = calendar.getByTestId('day-agenda');
  await expect(agenda.getByText('Сходини гуртка')).toBeVisible();
  await expect(agenda.getByText('17:00')).toBeVisible();
  await expect(calendar.locator(`[data-day="${localDay(soon(1, 17))}"] .dots i`)).toHaveCount(2); // the сходини and the task running that week
  await shot(page, info.project.name, 'agenda-calendar-month');

  // «Графіки гуртків» is on by default; off, the other гурток's сходини go.
  await pickDay(page, soon(2, 18));
  await expect(agenda.getByText('Графік куреня · Орли')).toBeVisible();
  await calendar.getByTestId('schedules').click();
  await expect(agenda.getByText('Графік куреня · Орли')).toHaveCount(0);
  expect(reads.some((r) => r.path === 'agenda/k1' && r.query.get('includeSchedules') === 'false')).toBe(true);
  await calendar.getByTestId('schedules').click();

  await calendar.locator('ion-segment-button[value=list]').click();
  await expect(calendar.getByTestId('list-day').first()).toBeVisible();
  await expect(calendar.getByText('Мандрівка на Говерлу').first()).toBeVisible();
  await expect(calendar.getByText('Завтра')).toBeVisible();
  await shot(page, info.project.name, 'agenda-calendar-list');
});

test('opens an event, answers it for the occurrence and sees who answered', async ({ page }, info) => {
  const { extra } = agendaApi();
  const log = await signIn(page, member, extra);
  await openCalendar(page);
  await pickDay(page, soon(1, 17));
  await page.locator('app-calendar').getByTestId('day-agenda').getByText('Сходини гуртка').click();

  const event = page.locator('app-event');
  await expect(event.getByTestId('item-title')).toHaveText('Сходини гуртка');
  await expect(event.getByText('Пласт-дім, Львів')).toBeVisible();
  await expect(event.getByText(/^Щотижня: /)).toBeVisible();
  await expect(event.getByText('Вузли і карта. Принеси зошит.')).toBeVisible();
  await expect(event.getByTestId('rsvp-list')).toContainText('Марко Шевчук');
  await expect(event.getByTestId('rsvp-counts')).toContainText('5');
  await expect(event.getByTestId('edit')).toHaveCount(0);
  await expect(event.getByTestId('delete')).toHaveCount(0);
  await expect(event.getByTestId('attendance')).toHaveCount(0);

  await event.getByTestId('rsvp').locator('ion-segment-button').filter({ hasText: /^Йду$/ }).click();
  await expect(event.getByTestId('rsvp-counts')).toContainText('6');
  const answer = log.writes.find((w) => w.path === 'agenda/e1/response');
  expect(answer?.body.status).toBe('Going');
  expect(new Date(answer?.body.occurrenceStartUtc).getTime()).toBe(new Date(soon(1, 17)).getTime());
  await shot(page, info.project.name, 'agenda-event');
});

test('lets the провід edit, score and delete a series', async ({ page }, info) => {
  const { extra } = agendaApi(true);
  const log = await signInAsLeader(page, extra, `./tabs/calendar/event/e1?start=${encodeURIComponent(soon(1, 17))}`);
  const event = page.locator('app-event');
  await expect(event.getByTestId('item-title')).toHaveText('Сходини гуртка');
  await expect(event.getByTestId('edit')).toBeVisible();
  await expect(event.getByTestId('attendance')).toBeVisible();
  await expect(event.getByTestId('attendance')).toHaveAttribute('href', /\/tabs\/calendar\/attendance\/e1\?start=/);
  await event.locator('ion-content').evaluate((el: HTMLIonContentElement) => el.scrollToBottom(0));
  await shot(page, info.project.name, 'agenda-event-leader');

  await event.getByTestId('delete').click();
  await expect(page.getByText('«Сходини гуртка» повторюється. Видалити всю серію?')).toBeVisible();
  await page.getByRole('button', { name: 'Видалити всі повторення' }).click();
  await expect.poll(() => log.writes.find((w) => w.method === 'DELETE')?.path).toBe('agenda/e1');
  await expect(page.locator('app-calendar')).toBeVisible();
});

test('creates an event for a гурток on the chosen day', async ({ page }, info) => {
  const { extra } = agendaApi(true);
  const log = await signInAsLeader(page, extra, './tabs/calendar');
  const calendar = page.locator('app-calendar');
  await pickDay(page, soon(2, 9));
  await calendar.getByTestId('new-event').click();

  const form = page.locator('app-event-form');
  await expect(form.locator('ion-title').first()).toHaveText('Нова подія');
  await expect(form.getByTestId('save')).toHaveAttribute('disabled', '');
  await form.locator('[data-testid=title] input').fill('Ватра');
  await form.locator('[data-testid=location] input').fill('Стрийський парк');
  await form.getByTestId('targets').click();
  const picker = page.locator('ion-modal').filter({ hasText: 'Для кого' });
  await expect(picker.getByText('Провід Соколів')).toBeVisible();
  await picker.locator('ion-checkbox').filter({ hasText: 'Увесь гурток' }).first().click();
  await shot(page, info.project.name, 'agenda-targets');
  await picker.getByTestId('targets-done').click();
  await expect(form.getByTestId('targets')).toContainText('Соколи');
  await shot(page, info.project.name, 'agenda-event-form');

  await form.getByTestId('save').click();
  await expect.poll(() => log.writes.find((w) => w.method === 'POST' && w.path === 'agenda')?.body).toMatchObject({
    kurinKey: 'k1',
    kind: 'Event',
    title: 'Ватра',
    location: 'Стрийський парк',
    isAllDay: true,
    startUtc: `${localDay(soon(2, 9))}T00:00:00.000Z`,
    recurrenceFrequency: 'None',
    targets: [{ targetType: 'Group', targetKey: 'g1', completionMode: 'Shared' }],
  });
  await expect(calendar).toBeVisible();
});

test('edits an event and keeps its targets', async ({ page }) => {
  const { extra } = agendaApi(true);
  const log = await signInAsLeader(page, extra, './tabs/calendar/edit/e2');
  const form = page.locator('app-event-form');
  await expect(form.locator('[data-testid=title] input')).toHaveValue('Мандрівка на Говерлу');
  await expect(form.getByTestId('targets')).toContainText('Курінь 7');
  await form.locator('[data-testid=title] input').fill('Мандрівка на Петрос');
  await form.getByTestId('save').click();
  await expect.poll(() => log.writes.find((w) => w.method === 'PUT' && w.path === 'agenda/e2')?.body).toMatchObject({
    agendaItemKey: 'e2',
    title: 'Мандрівка на Петрос',
    isAllDay: true,
    startUtc: `${localDay(soon(5, 8))}T00:00:00.000Z`,
    endUtc: `${localDay(soon(6, 8))}T00:00:00.000Z`,
    targets: [{ targetType: 'Kurin', targetKey: 'k1', completionMode: 'Shared' }],
  });
});

test('works the board: columns, search, «Моє», paging and a status move', async ({ page }, info) => {
  const { extra, reads } = agendaApi();
  const log = await signIn(page, member, extra);
  await page.locator('ion-tab-button[tab=tasks]').click();
  const board = page.locator('app-tasks');
  await expect(board.getByTestId('columns')).toContainText('Зробити 21');
  await expect(board.getByTestId('columns')).toContainText('В процесі 1');
  await expect(board.getByText('Вивчити вузли')).toBeVisible();
  await expect(board.getByTestId('new-task')).toHaveCount(0);
  await shot(page, info.project.name, 'agenda-tasks');

  await board.getByTestId('load-more').click();
  await expect(board.getByText('Задача 20')).toBeVisible();
  expect(reads.some((r) => r.path === 'agenda/k1/board' && r.query.get('status') === 'Todo' && r.query.get('skip') === '20')).toBe(true);
  await expect(board.getByTestId('load-more')).toHaveCount(0);

  await board.locator('ion-segment-button').filter({ hasText: 'В процесі' }).click();
  await expect(board.getByText('Принести намет')).toBeVisible();
  await expect(board.getByText(/^прострочено, /)).toBeVisible();
  await board.locator('ion-segment-button').filter({ hasText: 'Зробити' }).click();

  await board.getByTestId('mine').click();
  await expect(board.getByTestId('columns')).toContainText('Зробити 1');
  expect(reads.some((r) => r.path === 'agenda/k1/board' && r.query.get('onlyMine') === 'true')).toBe(true);
  await board.getByTestId('mine').click();

  await board.locator('[data-testid=search] input').fill('намет');
  await expect(board.getByTestId('columns')).toContainText('Зробити 0');
  await board.locator('[data-testid=search] input').fill('');
  await expect(board.getByTestId('columns')).toContainText('Зробити 21');

  // A tap on the mark offers the other columns.
  await board.getByTestId('task-row').filter({ hasText: 'Вивчити вузли' }).getByTestId('status').click();
  await page.getByRole('button', { name: 'В процесі' }).click();
  await expect.poll(() => log.writes.find((w) => w.path === 'agenda/t1/status')?.body).toEqual({ status: 'InProgress' });

  // A swipe does the same.
  const row = board.getByTestId('task-row').filter({ hasText: 'Задача 1' }).first();
  await row.scrollIntoViewIfNeeded();
  await row.evaluate((el: HTMLIonItemSlidingElement) => el.open('start'));
  await row.locator('ion-item-option').filter({ hasText: 'В процесі' }).dispatchEvent('click');
  await expect.poll(() => log.writes.find((w) => w.path === 'agenda/tt0/status')?.body).toEqual({ status: 'InProgress' });
});

test('shows a task with its progress and moves the own part', async ({ page }, info) => {
  const { extra } = agendaApi();
  const log = await signIn(page, member, extra);
  await page.locator('ion-tab-button[tab=tasks]').click();
  await page.locator('app-tasks').getByText('Вивчити вузли').click();

  const task = page.locator('app-task');
  await expect(task.getByTestId('item-title')).toHaveText('Вивчити вузли');
  await expect(task.getByTestId('target')).toContainText('1 з 2');
  await expect(task.getByTestId('target')).toContainText('Кожному окремо');
  await expect(task.getByTestId('own-part')).toBeVisible();
  await expect(task.getByTestId('edit')).toHaveCount(0);
  await shot(page, info.project.name, 'agenda-task');

  await task.getByTestId('own-part').locator('ion-segment-button').filter({ hasText: 'Зроблено' }).click();
  await expect.poll(() => log.writes.find((w) => w.path === 'agenda/t1/status')?.body).toEqual({ status: 'Done' });
});

test('lets the провід tick a person off and archive or delete from the board', async ({ page }, info) => {
  const { extra } = agendaApi(true);
  const log = await signInAsLeader(page, extra, './tabs/tasks/task/t1');
  const task = page.locator('app-task');
  await expect(task.getByTestId('part')).toHaveCount(2);
  await expect(task.getByTestId('own-part')).toHaveCount(0);
  await task.getByTestId('part').filter({ hasText: 'Марко Шевчук' }).locator('ion-checkbox').click();
  await expect.poll(() => log.writes.find((w) => w.path === 'agenda/t1/assignments/a1/status')?.body).toEqual({
    status: 'Done',
    memberKey: 'm2',
  });
  await shot(page, info.project.name, 'agenda-task-leader');

  await page.goto('./tabs/tasks');
  const board = page.locator('app-tasks');
  await expect(board.getByTestId('new-task')).toBeVisible();
  const row = board.getByTestId('task-row').filter({ hasText: 'Задача 2' }).first();
  await row.scrollIntoViewIfNeeded();
  await row.evaluate((el: HTMLIonItemSlidingElement) => el.open('end'));
  await row.locator('ion-item-option').filter({ hasText: 'Ще' }).dispatchEvent('click');
  await page.getByRole('button', { name: 'В архів' }).click();
  await expect.poll(() => log.writes.find((w) => w.path === 'agenda/tt1/archive')?.body).toEqual({ archived: true });

  const other = board.getByTestId('task-row').filter({ hasText: 'Задача 3' }).first();
  await other.scrollIntoViewIfNeeded();
  await other.evaluate((el: HTMLIonItemSlidingElement) => el.open('end'));
  await other.locator('ion-item-option').filter({ hasText: 'Видалити' }).dispatchEvent('click');
  await page.getByRole('button', { name: 'Видалити задачу' }).click();
  await expect.poll(() => log.writes.find((w) => w.method === 'DELETE')?.path).toBe('agenda/tt2');
});

test('creates a task done by each person', async ({ page }, info) => {
  const { extra } = agendaApi(true);
  const log = await signInAsLeader(page, extra, './tabs/tasks');
  await page.locator('app-tasks').getByTestId('new-task').click();
  const form = page.locator('app-task-form');
  await expect(form.locator('ion-segment-button[value=Task]')).toHaveClass(/segment-button-checked/);
  await form.locator('[data-testid=title] input').fill('Вивчити гімн');
  // The row sits at the foot of the form; bring it into the open first.
  await form.locator('ion-content').evaluate((el: HTMLIonContentElement) => el.scrollToBottom(0));
  await form.getByTestId('targets').click();
  const picker = page.locator('ion-modal').filter({ hasText: 'Для кого' });
  await picker.locator('ion-searchbar input').fill('сокол');
  await picker.locator('ion-checkbox').filter({ hasText: 'Увесь гурток' }).click();
  await picker.getByTestId('targets-done').click();
  await form.locator('ion-radio').filter({ hasText: 'Кожному окремо' }).click();
  await shot(page, info.project.name, 'agenda-task-form');
  await form.getByTestId('save').click();
  await expect.poll(() => log.writes.find((w) => w.method === 'POST' && w.path === 'agenda')?.body).toMatchObject({
    kind: 'Task',
    title: 'Вивчити гімн',
    location: null,
    agendaCategoryKey: null,
    targets: [{ targetType: 'Group', targetKey: 'g1', completionMode: 'PerMember' }],
  });
});
