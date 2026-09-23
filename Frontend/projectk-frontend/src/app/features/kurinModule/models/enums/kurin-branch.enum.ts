/**
 * Пластова гілка куреня. Проби й вмілості — юнацькі речі, тож поза УПЮ їх не показуємо.
 */
export enum KurinBranch {
  UPYu = 'UPYu',
  USP = 'USP',
  UPS = 'UPS'
}

export const KURIN_BRANCH_LABELS: Record<KurinBranch, string> = {
  [KurinBranch.UPYu]: 'УПЮ',
  [KurinBranch.USP]: 'УСП',
  [KurinBranch.UPS]: 'УПС'
};

/** Чи має ця гілка юнацький вишкіл — проби, вмілості, точкування. */
export function hasYouthProgram(branch: KurinBranch | null | undefined): boolean {
  return (branch ?? KurinBranch.UPYu) === KurinBranch.UPYu;
}
