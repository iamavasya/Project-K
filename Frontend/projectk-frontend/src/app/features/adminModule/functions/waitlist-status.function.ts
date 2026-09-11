export type WaitlistStatusSeverity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined;

export function getWaitlistStatusLabel(status: string | number): string {
  const normalized = normalizeWaitlistStatus(status);
  switch (normalized) {
    case 'Submitted':
      return 'Submitted';
    case 'NeedsManualVerification':
      return 'Verification Required';
    case 'Verified':
      return 'Verified';
    case 'Rejected':
      return 'Rejected';
    case 'ApprovedForInvitation':
      return 'Approved';
    default:
      return `Unknown (${String(status)})`;
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
