import { AgendaItemStatus, AgendaRsvpStatus } from '../../kurinModule/models/agenda';

/** The kurin a row belongs to; named on screen only when the person stands in more than one. */
export interface MyKurinRefDto {
  kurinKey: string;
  kurinNumber: number;
  namedAfter: string | null;
  isCurrent: boolean;
}

export interface MyEventDto {
  agendaItemKey: string;
  kurin: MyKurinRefDto;
  title: string;
  startUtc: string;
  endUtc: string | null;
  isAllDay: boolean;
  isRecurring: boolean;
  categoryName: string | null;
  categoryColorHex: string | null;
  categoryIcon: string | null;
  rsvpRequired: boolean;
  myResponse: AgendaRsvpStatus | null;
}

export interface MyTaskDto {
  agendaItemKey: string;
  kurin: MyKurinRefDto;
  title: string;
  status: AgendaItemStatus;
  startUtc: string | null;
  endUtc: string | null;
  addressedToMe: boolean;
  canChangeStatus: boolean;
}
