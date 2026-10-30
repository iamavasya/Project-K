/** A picture of a project's description: its key goes into the Markdown, its link expires. */
export interface ProbeProjectImageDto {
  imageKey: string;
  url: string;
}

/** One project of the third probe (`GET member/{key}/probes/{probeId}/projects`). */
export interface ProbeProjectDto {
  projectKey: string;
  title: string;
  isIndividual: boolean;
  /** Markdown; pictures are written as `project-image:{imageKey}`. */
  description: string;
  directionMentorName: string | null;
  directionMentorContact: string | null;
  updatedAtUtc: string;
  updatedByName: string | null;
  images: ProbeProjectImageDto[];
}

/** The limits the server holds, so the page names the same numbers. */
export interface ProbeProjectLimitsDto {
  maxProjects: number;
  maxImagesPerProject: number;
  maxImageBytes: number;
  maxDescriptionLength: number;
}

export interface ProbeProjectsDto {
  /** Whether this probe stands at the third place of the person's line; only that one has projects. */
  isProjectProbe: boolean;
  /** The person themself, or whoever the kurin lets sign their probe. */
  canEdit: boolean;
  projects: ProbeProjectDto[];
  limits: ProbeProjectLimitsDto;
}

export interface SaveProbeProjectRequestDto {
  title: string;
  isIndividual: boolean;
  description: string;
  directionMentorName: string | null;
  directionMentorContact: string | null;
}

/** The scheme of a picture's target in the Markdown. */
export const PROJECT_IMAGE_SCHEME = 'project-image:';
