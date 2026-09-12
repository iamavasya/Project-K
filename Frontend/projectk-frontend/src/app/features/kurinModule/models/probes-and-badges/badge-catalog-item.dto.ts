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
}