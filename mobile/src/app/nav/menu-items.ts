import { AuthState } from '../auth/auth.models';
import { kurinAccess } from '../features/kurin/kurin.labels';

/** One row of the menu: a screen of the app, or a page of the web (opened in the browser). */
export interface MenuEntry {
  key: string;
  label: string;
  /** Ionicons name. */
  icon: string;
  /** The iOS settings tile's colour. */
  tile: string;
  /** A screen of the app. */
  link?: string;
  /** A page outside the app: the web's own (a path on this origin) or the docs. */
  web?: string;
  /** The help, which is another site. */
  help?: true;
}

/** What the menu needs beyond the session: the kurin's branch and the гуртки whose box one keeps. */
export interface MenuContext {
  youthKurin: boolean;
  duesGroups: { groupKey: string; groupName: string }[];
}

const has = (state: AuthState, prefix: string) => state.permissions.some((p) => p.startsWith(prefix));

/**
 * The web's sidebar (kurinModule/components/sidebar-menu buildItems), item for item: the same names,
 * the same order, the same rights. What the phone has a screen for opens here; the rest opens the web.
 */
export function menuEntries(state: AuthState | null, context: MenuContext): MenuEntry[] {
  if (!state) return [];
  const kurinKey = state.kurinKey ?? null;
  const access = kurinAccess(state);
  const isAdmin = state.isAdmin;
  const canSeeRegistry = isAdmin || access.manageWholeKurin || has(state, 'Group:Update');
  const canManageKurinSettings = isAdmin || has(state, 'Kurin:Update:KurinWide');
  const canSeeKurinDues = isAdmin || has(state, 'KurinDues:Read');
  const items: MenuEntry[] = [];

  if (state.memberKey) {
    items.push({ key: 'home', label: 'Головна', icon: 'home', tile: '#007aff', link: '/tabs/home' });
    items.push({ key: 'profile', label: 'Мій профіль', icon: 'person', tile: '#5856d6', link: '/tabs/more/profile' });
  }

  if (kurinKey) {
    items.push({ key: 'kurin', label: 'Курінь', icon: 'flag', tile: '#ff9500', link: '/tabs/kurin' });
    if (canSeeRegistry) {
      items.push({ key: 'registry', label: 'Реєстр', icon: 'grid', tile: '#8e8e93', web: '/kurin/registry' });
    }
    if (canManageKurinSettings) {
      items.push({ key: 'import', label: 'Імпорт складу', icon: 'cloud-upload', tile: '#8e8e93', web: '/kurin/import' });
    }
    items.push({ key: 'calendar', label: 'Календар', icon: 'calendar', tile: '#ff3b30', link: '/tabs/calendar' });
    items.push({ key: 'tasks', label: 'Задачі', icon: 'checkbox', tile: '#34c759', link: '/tabs/tasks' });
    items.push({ key: 'planning', label: 'Планування', icon: 'time', tile: '#8e8e93', web: `/planning/${kurinKey}` });
    items.push({ key: 'score', label: 'Точкування', icon: 'trophy', tile: '#f4b400', link: '/tabs/kurin/score' });
    if (access.reviewSkills && context.youthKurin) {
      items.push({
        key: 'review',
        label: 'Модерація вмілостей',
        icon: 'ribbon',
        tile: '#af52de',
        link: '/tabs/kurin/review/skills',
      });
    }
    // One гурток: straight to its box; several: an entry each, as the web's fold has them.
    const groups = access.seeGroupDues ? context.duesGroups : [];
    for (const group of groups) {
      items.push({
        key: `dues-${group.groupKey}`,
        label: groups.length === 1 ? 'Вкладка гуртка' : `Вкладка: ${group.groupName}`,
        icon: 'wallet',
        tile: '#34a853',
        link: `/tabs/kurin/group/${group.groupKey}/dues`,
      });
    }
    if (canSeeKurinDues) {
      items.push({ key: 'kurin-dues', label: 'Вкладка куреня', icon: 'wallet', tile: '#8e8e93', web: `/kurin/${kurinKey}/dues` });
    }
    if (canManageKurinSettings) {
      items.push({
        key: 'kurin-settings',
        label: 'Налаштування куреня',
        icon: 'settings',
        tile: '#8e8e93',
        web: `/kurin/${kurinKey}/settings`,
      });
    }
  }

  if (isAdmin && !kurinKey) {
    items.push({ key: 'panel', label: 'Адміністрація', icon: 'key', tile: '#8e8e93', web: '/panel' });
    items.push({ key: 'users', label: 'Користувачі', icon: 'people', tile: '#8e8e93', web: '/users' });
    items.push({ key: 'system', label: 'Системні налаштування', icon: 'options', tile: '#8e8e93', web: '/system-settings' });
  }

  items.push({ key: 'account', label: 'Налаштування акаунта', icon: 'shield-checkmark', tile: '#34a853', link: '/tabs/more/account' });
  items.push({ key: 'help', label: 'Довідка', icon: 'book', tile: '#0a84ff', help: true });
  items.push({ key: 'report', label: 'Повідомити про проблему', icon: 'bug', tile: '#ff3b30', link: '/tabs/more/report' });
  items.push({ key: 'about', label: 'Про Лілейку', icon: 'information-circle', tile: '#8e9a95', link: '/tabs/more/about' });
  return items;
}

/** The entry the current URL is under; the longest link wins, so «Курінь» does not light up beside «Точкування». */
export function currentEntry(entries: MenuEntry[], url: string): string | null {
  const path = url.split(/[?#]/)[0];
  let best: MenuEntry | null = null;
  for (const entry of entries) {
    const link = entry.link;
    if (!link || !(path === link || path.startsWith(`${link}/`))) continue;
    if (!best?.link || link.length > best.link.length) best = entry;
  }
  return best?.key ?? null;
}
