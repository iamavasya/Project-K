export const ENTITY_CACHE_TTL_MS = 60 * 1000;
export const CATALOG_CACHE_TTL_MS = 6 * 60 * 60 * 1000;

export const MEMBER_CACHE_PREFIX = 'member:';
export const GROUP_CACHE_PREFIX = 'group:';
export const KURIN_CACHE_PREFIX = 'kurin:';
export const LEADERSHIP_CACHE_PREFIX = 'leadership:';
export const PLANNING_CACHE_PREFIX = 'planning:';
export const MEMBER_WARNING_CACHE_PREFIX = 'member-warning:';
export const MEMBER_PROGRESS_CACHE_PREFIX = 'member-progress:';
export const BADGES_CATALOG_CACHE_PREFIX = 'catalog:badges:';
export const PROBES_CATALOG_CACHE_PREFIX = 'catalog:probes:';
export const LAYOUT_CACHE_PREFIX = 'layout:';

/**
 * Everything a kurin scope can colour. Catalogues are deliberately absent: they are
 * global reference data and must survive a scope switch or a sign-out.
 */
export const KURIN_SCOPED_CACHE_PREFIXES = [
  MEMBER_CACHE_PREFIX,
  GROUP_CACHE_PREFIX,
  KURIN_CACHE_PREFIX,
  LEADERSHIP_CACHE_PREFIX,
  PLANNING_CACHE_PREFIX,
  MEMBER_WARNING_CACHE_PREFIX,
  MEMBER_PROGRESS_CACHE_PREFIX,
  LAYOUT_CACHE_PREFIX
];
