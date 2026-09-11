import { PlastLevel } from "./enums/plast-level.enum";

export interface PlastLevelHistoryDto {
    memberKey?: string;
    plastLevelHistoryKey?: string;
    plastLevel: PlastLevel;
    dateAchieved?: Date | string | null;
}