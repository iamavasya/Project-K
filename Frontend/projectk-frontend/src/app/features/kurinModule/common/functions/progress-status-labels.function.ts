import { BadgeProgressStatus } from '../models/enums/badge-progress-status.enum';
import { ProbeProgressStatus } from '../models/enums/probe-progress-status.enum';

export function getBadgeProgressStatusLabel(status: BadgeProgressStatus): string {
  switch (status) {
    case BadgeProgressStatus.Submitted:
      return 'Вже подано, очікує підтвердження';
    case BadgeProgressStatus.Confirmed:
      return 'Вже підтверджено';
    case BadgeProgressStatus.Rejected:
      return 'Було відхилено. Можна подати повторно';
    default:
      return 'Вже додано';
  }
}

export function getProbeProgressStatusLabel(status: ProbeProgressStatus): string {
  switch (status) {
    case ProbeProgressStatus.InProgress:
      return 'В процесі';
    case ProbeProgressStatus.Completed:
      return 'Завершено';
    case ProbeProgressStatus.Verified:
      return 'Підтверджено';
    default:
      return 'Не розпочато';
  }
}

export function getBadgeProgressShortStatusLabel(status: BadgeProgressStatus): string | null {
  switch (status) {
    case BadgeProgressStatus.Submitted:
      return 'Очікує підтвердження';
    case BadgeProgressStatus.Confirmed:
      return 'Підтверджено';
    case BadgeProgressStatus.Rejected:
      return 'Відхилено';
    default:
      return null;
  }
}
