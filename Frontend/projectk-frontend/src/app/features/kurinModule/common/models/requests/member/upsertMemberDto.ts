export interface UpsertMemberDto {
    groupKey?: string;
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth: string;
}