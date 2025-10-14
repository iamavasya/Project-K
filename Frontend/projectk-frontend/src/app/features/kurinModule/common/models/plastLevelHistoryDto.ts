import { PlastLevel } from "./enums/plast-level.enum";

export interface PlastLevelHistoryDto {
    level: PlastLevel;
    date?: string | null;
}