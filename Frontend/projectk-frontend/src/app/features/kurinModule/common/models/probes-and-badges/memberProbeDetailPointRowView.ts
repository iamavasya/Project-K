export interface MemberProbeDetailPointRowView {
  sectionId: string;
  sectionCode: string;
  sectionTitle: string;
  pointId: string;
  pointTitle: string;
  isSigned: boolean;
  signedByName: string | null;
  signedByRole: string | null;
  signedAtUtc: string | null;
}
