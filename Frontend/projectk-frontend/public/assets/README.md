# Brand assets — source of truth

This folder is the canonical home for the **Лілейка** brand marks. Any change to
the logo/favicon happens here first, then the two hand-inlined copies in the app
must be kept in sync (see below). Colour and usage rules live in
[`BRANDBOOK.md`](../../../../BRANDBOOK.md) §1.

## The mark

A **three-petal lily** with a plain vertical stem — **no crossbar**. The centre
petal is **terracotta** (`#D9762F`, dark `#EE9A5A`); the two side petals and the
stem are green (`#0E6E4E`, dark `#34C48D`) or `currentColor`. The former
two-petal variant is retired — it read ambiguously and must not be reproduced.

Core paths (viewBox `0 0 100 100`):

| Part | Path |
|---|---|
| Stem | `M50 92 V46` |
| Left petal | `M50 46 C28 44 18 30 26 18 C36 8 48 24 50 46 Z` |
| Right petal | `M50 46 C72 44 82 30 74 18 C64 8 52 24 50 46 Z` |
| Centre petal (terracotta) | `M50 46 C42 30 44 16 50 8 C56 16 58 30 50 46 Z` |

## Files

| File | Size | Use | Notes |
|---|---|---|---|
| `lilyka-mark.svg` | 100×100 | primary two-colour mark | green + terracotta centre |
| `lilyka-mark-dark.svg` | 100×100 | dark backgrounds | `#34C48D` + `#EE9A5A` |
| `lilyka-mark-mono.svg` | 100×100 | print / single-colour | all `currentColor` (intentional mono exception) |
| `favicon.svg` | 64×64 | `<link rel="icon">` | white mark + terracotta centre on green tile `r=14` |
| `favicon-16.svg` | 16×16 | small favicon | simplified, no stem, terracotta centre |
| `lilyka-banner-1080x288.svg` | 1080×288 | README banner | centre `#F5C99B` for contrast on green |
| `lilyka-og-1200x630.svg` | 1200×630 | `og:image` | cream background, terracotta centre |
| `images/scouts-main.png` | — | welcome hero photo | not a brand mark |

`favicon.ico` (root of `public/`) is the unused Angular default and is not
referenced by `index.html`.

## In-app copies to keep in sync

The mark is inlined (not a shared component, to preserve the descendant CSS that
sizes and colours it). When the geometry changes, update **both**:

- `src/app/features/kurinModule/common/components/sidebar-menu/sidebar-menu.html`
- `src/app/features/systemModule/components/welcome-page/welcome-page.html`

The centre petal there carries `class="lil-mark-center"`, styled globally in
`src/lilyka-theme.css` (`stroke: var(--lil-clay-500)`), so it stays terracotta
across light/dark themes while the rest of the mark follows `currentColor`.
