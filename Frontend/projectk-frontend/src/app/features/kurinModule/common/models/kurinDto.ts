export interface KurinDto {
  kurinKey: string;
  number: number;
  managerEmail?: string;
  name?: string;
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  isZbtEnabled?: boolean;
  zbtUserCap?: number;
  currentUserCount?: number;
}
