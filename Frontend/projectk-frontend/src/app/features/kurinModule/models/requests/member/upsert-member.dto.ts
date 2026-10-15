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
    /** Values of the край's own card fields by key; an empty one clears it. */
    cardExtras?: Record<string, string>;
    removeProfilePhoto?: boolean;
    plastLevelHistories?: PlastLevelHistoryDto[];
}
