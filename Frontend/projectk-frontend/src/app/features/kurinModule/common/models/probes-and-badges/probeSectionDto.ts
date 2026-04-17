import { ProbePointDto } from './probePointDto';

export interface ProbeSectionDto {
  id: string;
  code: string;
  title: string;
  points: ProbePointDto[];
}