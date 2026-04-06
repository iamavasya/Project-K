export interface MemberLookupDto {
    memberKey: string;
    firstName: string;
    middleName: string;
    lastName: string;
    profilePhotoUrl?: string | null;
    latestPlastLevel?: string | null;
    latestPlastLevelDisplay?: string | null;
    phoneNumber?: string | null;
    dateOfBirth?: string | Date | null;
}