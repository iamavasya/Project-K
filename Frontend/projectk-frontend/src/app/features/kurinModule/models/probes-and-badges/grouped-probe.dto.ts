import { ProbeSectionDto } from './probe-section.dto';

export interface GroupedProbeDto {
  id: string;
  title: string;
  pointsCount: number;
  sectionsCount: number;
  sections: ProbeSectionDto[];
}