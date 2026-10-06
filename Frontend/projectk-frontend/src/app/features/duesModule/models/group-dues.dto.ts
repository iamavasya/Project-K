import { DuesEntryKind, DuesPaymentMethod } from './dues.enums';

export interface QuarterDto {
  year: number;
  number: number;
}

export interface PlastYearDto {
  startYear: number;
  label: string;
  quarters: QuarterDto[];
}

export interface KurinDuesRateDto {
  fromQuarter: QuarterDto;
  stanytsiaFull: number;
  stanytsiaReduced: number;
  kurinShare: number;
}

export interface GroupDuesRateDto {
  fromQuarter: QuarterDto;
  groupShare: number;
}

export interface DuesAmountDto {
  stanytsia: number;
  kurin: number;
  group: number;
  total: number;
}

export interface DuesAccountQuarterDto {
  quarter: QuarterDto;
  charged: DuesAmountDto;
  paid: DuesAmountDto;
  balance: number;
  isConcession: boolean;
}

export type DuesAccountStanding = 'Current' | 'Moved' | 'Left';

export interface DuesAccountDto {
  membershipKey: string;
  memberKey: string;
  fullName: string;
  standing: DuesAccountStanding;
  isConcessionNow: boolean;
  quarters: DuesAccountQuarterDto[];
  charged: number;
  payments: number;
  balance: number;
}

export interface DuesEntryDto {
  duesEntryKey: string;
  kind: DuesEntryKind;
  method: DuesPaymentMethod;
  counterMethod: DuesPaymentMethod | null;
  amount: number;
  occurredOn: string;
  membershipKey: string | null;
  memberName: string | null;
  collectedByMemberKey: string | null;
  collectedByName: string | null;
  note: string | null;
  isVerified: boolean;
  verifiedAtUtc: string | null;
  verifiedByName: string | null;
  receivedAtUtc: string | null;
  createdAtUtc: string;
}

export interface DuesBoxDto {
  cash: number;
  card: number;
  total: number;
  toForward: number;
  own: number;
  inTransit: number;
}

export interface DuesHandoverDto {
  owedUp: number;
  transferred: number;
  received: number;
  outstanding: number;
}

export interface DuesPersonDto {
  memberKey: string;
  fullName: string;
}

export interface DuesViewerDto {
  canKeep: boolean;
  canVerify: boolean;
  canSetKurinRates: boolean;
}

/** `GET api/group/{groupKey}/dues` — the гурток's whole box in one read. */
export interface GroupDuesDto {
  groupKey: string;
  kurinKey: string;
  groupName: string;
  currentQuarter: QuarterDto;
  years: PlastYearDto[];
  kurinRates: KurinDuesRateDto[];
  groupRates: GroupDuesRateDto[];
  accounts: DuesAccountDto[];
  entries: DuesEntryDto[];
  box: DuesBoxDto;
  handover: DuesHandoverDto;
  people: DuesPersonDto[];
  viewer: DuesViewerDto;
}

export interface UpsertDuesEntryRequest {
  kind: DuesEntryKind;
  method: DuesPaymentMethod;
  counterMethod: DuesPaymentMethod | null;
  amount: number;
  occurredOn: string;
  membershipKey: string | null;
  collectedByMemberKey: string | null;
  note: string | null;
}

export interface SetGroupDuesRateRequest {
  fromQuarter: QuarterDto;
  groupShare: number;
}

export interface SetKurinDuesRateRequest {
  fromQuarter: QuarterDto;
  stanytsiaFull: number;
  stanytsiaReduced: number;
  kurinShare: number;
}

export interface SetDuesConcessionRequest {
  fromQuarter: QuarterDto;
  isConcession: boolean;
}
