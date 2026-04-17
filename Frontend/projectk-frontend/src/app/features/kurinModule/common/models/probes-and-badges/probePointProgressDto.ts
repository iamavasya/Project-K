export interface ProbePointProgressDto {
  probePointProgressKey: string | null;
  pointId: string;
  isSigned: boolean;
  signedAtUtc: string | null;
  signedByUserKey: string | null;
  signedByName: string | null;
  signedByRole: string | null;
}
