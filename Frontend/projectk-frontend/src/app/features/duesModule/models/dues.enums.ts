import type { DuesAccountStanding } from './group-dues.dto';

/** Mirrors `DuesEntryKind` on the backend; enums travel as strings. */
export enum DuesEntryKind {
  Contribution = 'Contribution',
  Refund = 'Refund',
  TransferToKurin = 'TransferToKurin',
  TransferToStanytsia = 'TransferToStanytsia',
  Expense = 'Expense',
  OtherIncome = 'OtherIncome',
  Exchange = 'Exchange',
  Correction = 'Correction'
}

export enum DuesPaymentMethod {
  Cash = 'Cash',
  Card = 'Card'
}

export const DUES_ENTRY_KIND_LABELS: Record<DuesEntryKind, string> = {
  [DuesEntryKind.Contribution]: 'Вкладка',
  [DuesEntryKind.Refund]: 'Повернення',
  [DuesEntryKind.TransferToKurin]: 'Передача курінному',
  [DuesEntryKind.TransferToStanytsia]: 'Передача в станицю',
  [DuesEntryKind.Expense]: 'Витрата',
  [DuesEntryKind.OtherIncome]: 'Інший прихід',
  [DuesEntryKind.Exchange]: 'Обмін готівка ↔ картка',
  [DuesEntryKind.Correction]: 'Корекція'
};

export const DUES_PAYMENT_METHOD_LABELS: Record<DuesPaymentMethod, string> = {
  [DuesPaymentMethod.Cash]: 'Готівка',
  [DuesPaymentMethod.Card]: 'Картка'
};

/** Kinds that are about one person's вкладка and so name a membership. */
export const PERSONAL_DUES_KINDS: ReadonlySet<DuesEntryKind> = new Set([
  DuesEntryKind.Contribution,
  DuesEntryKind.Refund,
  DuesEntryKind.Correction
]);

/** What a гурток's box records; handing to the станиця is the kurin's. */
export const GROUP_DUES_KINDS: readonly DuesEntryKind[] = [
  DuesEntryKind.Contribution,
  DuesEntryKind.Refund,
  DuesEntryKind.Expense,
  DuesEntryKind.OtherIncome,
  DuesEntryKind.TransferToKurin,
  DuesEntryKind.Exchange,
  DuesEntryKind.Correction
];

/** What the kurin's own box records: nothing personal, and what goes up to the станиця. */
export const KURIN_DUES_KINDS: readonly DuesEntryKind[] = [
  DuesEntryKind.Expense,
  DuesEntryKind.OtherIncome,
  DuesEntryKind.TransferToStanytsia,
  DuesEntryKind.Exchange
];

/** How a person stands towards a гурток's box, in a word; one who is still here needs none. */
export const DUES_STANDING_LABELS: Record<DuesAccountStanding, string | null> = {
  Current: null,
  Moved: 'переведений',
  Left: 'вибув'
};

/** Kinds that bring money into the box, as the history shows them with a plus. */
export const INCOMING_DUES_KINDS: ReadonlySet<DuesEntryKind> = new Set([
  DuesEntryKind.Contribution,
  DuesEntryKind.OtherIncome
]);
