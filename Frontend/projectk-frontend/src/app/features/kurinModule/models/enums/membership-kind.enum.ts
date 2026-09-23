/** Чим людина є в цьому курені: юнацтвом чи виховним складом. */
export enum MembershipKind {
  Youth = 'Youth',
  Staff = 'Staff'
}

export const MEMBERSHIP_KIND_LABELS: Record<MembershipKind, string> = {
  [MembershipKind.Youth]: 'Юнацтво',
  [MembershipKind.Staff]: 'Виховний склад'
};
