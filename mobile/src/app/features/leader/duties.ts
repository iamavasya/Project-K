import { dayLabel, timeLabel } from '../../me/labels';
import { MyDutyDto, MyDutyKind } from '../../me/me.models';

/** One line of the «Справи» card: what to do, where, how many, and the screen it is done on. */
export interface DutyRow {
  key: string;
  icon: string;
  /** The icon tile's colour on iPhone. */
  tile: string;
  label: string;
  detail: string;
  /** Shown for everything but an event, which is one thing to do. */
  count: number | null;
  /** Null when the phone has no screen for it or it lies in another kurin: the row is said, not opened. */
  link: string | null;
  queryParams: Record<string, string> | null;
}

const META: Record<MyDutyKind, { icon: string; tile: string; label: string }> = {
  BadgesToReview: { icon: 'star', tile: '#f4b400', label: 'Вмілості на перевірку' },
  TransfersToConfirm: { icon: 'arrow-down', tile: '#5b8def', label: 'Передачі від гуртків підтвердити' },
  EntriesToVerify: { icon: 'wallet', tile: '#34a853', label: 'Операції перевірити' },
  EventWithoutAttendance: { icon: 'checkbox', tile: '#e8710a', label: 'Відмітити присутність' },
};

/**
 * The web's my-duties-tile rows (dashboardModule/components/my-duties-tile), linked to the phone's
 * screens. The kurin's own box (transfers, its entries) stays on the web, so those rows only say.
 */
export function dutyRows(duties: MyDutyDto[], today: Date, namesKurin: boolean): DutyRow[] {
  return duties.map((duty) => {
    const meta = META[duty.kind];
    const parts: string[] = [];
    if (duty.kind === 'EventWithoutAttendance' && duty.occurrenceStartUtc) {
      const start = new Date(duty.occurrenceStartUtc);
      parts.push(dayLabel(start, today));
      const time = timeLabel(start, null, false);
      if (time) parts.push(time);
    } else if (duty.groupName) {
      parts.push(duty.groupName);
    } else if (duty.kind === 'EntriesToVerify') {
      parts.push('каса куреня');
    }
    if (namesKurin) parts.push(`к. ч. ${duty.kurin.kurinNumber}`);

    const link = duty.kurin.isCurrent ? linkOf(duty) : null;
    return {
      key: [duty.kind, duty.kurin.kurinKey, duty.groupKey ?? '', duty.agendaItemKey ?? '', duty.occurrenceStartUtc ?? ''].join('|'),
      icon: meta.icon,
      tile: meta.tile,
      label: duty.kind === 'EventWithoutAttendance' && duty.title ? `${meta.label}: ${duty.title}` : meta.label,
      detail: parts.join(' · '),
      count: duty.kind === 'EventWithoutAttendance' ? null : duty.count,
      link,
      queryParams:
        link && duty.kind === 'EventWithoutAttendance' && duty.occurrenceStartUtc
          ? { start: duty.occurrenceStartUtc }
          : null,
    };
  });
}

/** The web names the kurin on each row when the person is in more than one. */
export function dutiesSpanKurins(duties: MyDutyDto[]): boolean {
  return new Set(duties.map((duty) => duty.kurin.kurinKey)).size > 1 || duties.some((duty) => !duty.kurin.isCurrent);
}

function linkOf(duty: MyDutyDto): string | null {
  switch (duty.kind) {
    case 'BadgesToReview':
      return '/tabs/kurin/review/skills';
    case 'EntriesToVerify':
      return duty.groupKey ? `/tabs/kurin/group/${duty.groupKey}/dues` : null;
    case 'EventWithoutAttendance':
      return duty.agendaItemKey ? `/tabs/calendar/attendance/${duty.agendaItemKey}` : null;
    case 'TransfersToConfirm':
      return null;
  }
}
