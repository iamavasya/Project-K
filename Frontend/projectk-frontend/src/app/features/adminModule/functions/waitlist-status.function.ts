export type WaitlistStatusSeverity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined;

/** The same words the phone app uses (mobile waitlist.labels WAITLIST_STATUS). */
export function getWaitlistStatusLabel(status: string | number): string {
  const normalized = normalizeWaitlistStatus(status);
  switch (normalized) {
    case 'Submitted':
      return 'Подано';
    case 'NeedsManualVerification':
      return 'Потрібна перевірка';
    case 'Verified':
      return 'Перевірено';
    case 'Rejected':
      return 'Відхилено';
    case 'ApprovedForInvitation':
      return 'Схвалено';
    default:
      return `Невідомо (${String(status)})`;
  }
}

export function getWaitlistStatusSeverity(status: string | number): WaitlistStatusSeverity {
  const normalized = normalizeWaitlistStatus(status);
  switch (normalized) {
    case 'Submitted':
      return 'info';
    case 'NeedsManualVerification':
      return 'warn';
    case 'Verified':
      return 'success';
    case 'Rejected':
      return 'danger';
    case 'ApprovedForInvitation':
      return 'success';
    default:
      return 'secondary';
  }
}

/** Whether an administrator still has to decide on this entry. */
export function isWaitlistAwaitingDecision(status: string | number): boolean {
  const normalized = normalizeWaitlistStatus(status);
  return normalized === 'Submitted' || normalized === 'NeedsManualVerification';
}

export function isWaitlistInitial(status: string | number): boolean {
  return normalizeWaitlistStatus(status) === 'Submitted';
}

export function isWaitlistApproved(status: string | number): boolean {
  return normalizeWaitlistStatus(status) === 'ApprovedForInvitation';
}

function normalizeWaitlistStatus(status: string | number): string {
  const normalized = String(status);
  switch (normalized) {
    case '0':
      return 'Submitted';
    case '1':
      return 'NeedsManualVerification';
    case '2':
      return 'Verified';
    case '3':
      return 'Rejected';
    case '4':
      return 'ApprovedForInvitation';
    default:
      return normalized;
  }
}
