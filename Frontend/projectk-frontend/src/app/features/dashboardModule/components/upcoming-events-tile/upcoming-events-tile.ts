import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { AgendaRsvpStatus } from '../../../kurinModule/models/agenda';
import { dayLabel, timeLabel } from '../../functions/day-label.function';
import { MyEventDto } from '../../models/me.dto';

export interface EventResponseChange {
  event: MyEventDto;
  status: AgendaRsvpStatus;
}

export interface EventRow {
  event: MyEventDto;
  day: string;
  time: string;
  /** The event asks for an answer and has none yet — what the tile puts first. */
  isAwaiting: boolean;
}

/** One row per occurrence: a series shows several rows under one item key, so the key alone is not a row. */
export function eventRowKey(event: Pick<MyEventDto, 'agendaItemKey' | 'startUtc'>): string {
  return `${event.agendaItemKey}|${event.startUtc}`;
}

export const RSVP_OPTIONS: readonly { value: AgendaRsvpStatus; label: string }[] = [
  { value: 'Going', label: 'Іду' },
  { value: 'Maybe', label: 'Можливо' },
  { value: 'NotGoing', label: 'Не йду' }
];

/**
 * The next two weeks across every kurin, with the person's answer right in the row. Events still
 * waiting for an answer come first: that is the one thing the tile asks of them.
 */
@Component({
  selector: 'app-upcoming-events-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, TooltipModule, EmptyStateComponent],
  templateUrl: './upcoming-events-tile.html',
  styleUrl: './upcoming-events-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpcomingEventsTileComponent {
  readonly events = input<MyEventDto[]>([]);
  readonly loading = input(false);
  readonly failed = input(false);
  /** Whether the person stands in more than one kurin: only then is the kurin worth naming on a row. */
  readonly namesKurin = input(false);
  readonly currentKurinKey = input<string | null>(null);
  readonly savingKey = input<string | null>(null);
  readonly respond = output<EventResponseChange>();

  readonly options = RSVP_OPTIONS;
  readonly today = new Date();

  readonly rows = computed<EventRow[]>(() =>
    this.events()
      .map(event => ({
        event,
        day: dayLabel(new Date(event.startUtc), this.today),
        time: timeLabel(new Date(event.startUtc), event.endUtc ? new Date(event.endUtc) : null, event.isAllDay),
        isAwaiting: event.rsvpRequired && event.myResponse === null
      }))
      .sort((a, b) => Number(b.isAwaiting) - Number(a.isAwaiting) || a.event.startUtc.localeCompare(b.event.startUtc))
  );

  readonly awaitingCount = computed(() => this.rows().filter(r => r.isAwaiting).length);

  rowKey(row: EventRow): string {
    return eventRowKey(row.event);
  }

  kurinLabel(row: EventRow): string {
    return `к. ч. ${row.event.kurin.kurinNumber}`;
  }

  calendarLink(): unknown[] | null {
    const key = this.currentKurinKey();
    return key ? ['/calendar', key] : null;
  }
}
