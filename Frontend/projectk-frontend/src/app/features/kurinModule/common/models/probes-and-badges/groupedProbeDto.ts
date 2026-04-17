import { ProbeSectionDto } from './probeSectionDto';

export interface GroupedProbeDto {
  id: string;
  title: string;
  pointsCount: number;
  sectionsCount: number;
  sections: ProbeSectionDto[];
}