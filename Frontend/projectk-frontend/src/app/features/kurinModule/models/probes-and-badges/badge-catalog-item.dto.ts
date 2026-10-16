export interface BadgeCatalogItemDto {
  id: string;
  title: string;
  imagePath: string;
  country: string;
  specialization: string;
  status: string;
  level: number | null;
  lastUpdated: string;
  seekerRequirements: string;
  instructorRequirements: string;
  fixNotes: string[];
  /** The catalogue's requirements one by one; absent from APIs older than the badge card. */
  requirements?: BadgeRequirementDto[];
  author?: string;
  ulady?: string[];
  /** The catalogue's code of the край the вмілість belongs to; empty when it names none. */
  kraiCode?: string;
}

export interface BadgeRequirementDto {
  id: string;
  text: string;
}