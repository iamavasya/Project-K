import { TemplateRef } from '@angular/core';

export type TileSpan = 'full' | 'half' | 'third';

export interface TileDefinition {
  key: string;
  span: TileSpan;
  pinned: boolean;
  defaultOrder: number;
  template: TemplateRef<unknown>;
  label: string;
}

export interface TileDefInput {
  key: string;
  span?: TileSpan;
  pinned?: boolean;
  label?: string;
}

/** What a person keeps about a board: the order they chose, and the tiles they took off it. */
export interface TileLayout {
  tileKeys: string[];
  hiddenTileKeys: string[];
}

export const TILE_LAYOUT_SCHEMA_VERSION = 1;

export const TILE_BOARD_KEYS = {
  memberCard: 'member-card',
  kurinPanel: 'kurin-panel',
  groupPanel: 'group-panel',
  dashboard: 'dashboard'
} as const;
