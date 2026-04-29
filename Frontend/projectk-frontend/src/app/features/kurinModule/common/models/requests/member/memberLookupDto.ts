export interface MemberLookupDto {
    memberKey: string;
    userKey?: string | null;
    firstName: string;
    middleName: string;
    lastName: string;
    profilePhotoUrl?: string | null;
    latestPlastLevel?: string | null;
    latestPlastLevelDisplay?: string | null;
    phoneNumber?: string | null;
    dateOfBirth?: string | Date | null;
}
