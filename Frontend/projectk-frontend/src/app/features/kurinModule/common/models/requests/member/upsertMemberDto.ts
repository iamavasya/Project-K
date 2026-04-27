import { PlastLevelHistoryDto } from "../../plastLevelHistoryDto";

export interface UpsertMemberDto {
    groupKey?: string;
    kurinKey?: string;
    createUserAccount?: boolean;
    firstName: string;
    middleName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth: string;
    removeProfilePhoto?: boolean;
    plastLevelHistories?: PlastLevelHistoryDto[];
}
