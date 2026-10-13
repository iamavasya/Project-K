import { PlastLevelHistoryDto } from "../../plast-level-history.dto";

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
    address?: string | null;
    school?: string | null;
    removeProfilePhoto?: boolean;
    plastLevelHistories?: PlastLevelHistoryDto[];
}
