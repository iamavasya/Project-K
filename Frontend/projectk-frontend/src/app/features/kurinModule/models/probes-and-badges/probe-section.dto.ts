import { ProbePointDto } from './probe-point.dto';

export interface ProbeSectionDto {
  id: string;
  code: string;
  title: string;
  points: ProbePointDto[];
}