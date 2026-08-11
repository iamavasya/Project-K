# Brand assets — source of truth

This folder is the canonical home for the **Лілейка** brand marks. Any change to
the logo/favicon happens here first, then the two hand-inlined copies in the app
must be kept in sync (see below). Colour and usage rules live in
[`BRANDBOOK.md`](../../../../BRANDBOOK.md) §1.

## The mark

A **scout fleur-de-lis** in a **line** style, **tilted 45° right**. Central pointed
petal is **terracotta** (`#D9762F`, dark `#EE9A5A`, `#F5C99B` on green tiles); the
two side scrolls, the single band and the lower point are green (`#0E6E4E`, dark
`#34C48D`) or `currentColor`. Earlier lily variants (two- and three-petal) are
retired and must not be reproduced.

Core paths (viewBox `0 0 100 100`, all wrapped in `<g transform="rotate(45 50 50)">`):

| Part | Path |
|---|---|
| Right scroll | `M50 44 C54 33 62 28 71 33 C79 38 80 48 73 53 C68 56 62 55 60 51` |
| Left scroll | `M50 44 C46 33 38 28 29 33 C21 38 20 48 27 53 C32 56 38 55 40 51` |
| Band | `M33 60 H67` |
| Lower point | `M50 88 C46 80 46 71 50 65 C54 71 54 80 50 88 Z` |
| Central petal (terracotta) | `M50 7 C45 24 45 43 50 57 C55 43 55 24 50 7 Z` |

## Files

| File | Size | Use | Notes |
|---|---|---|---|
| `lileyka-mark.svg` | 100×100 | primary two-colour mark | green + terracotta centre |
| `lileyka-mark-dark.svg` | 100×100 | dark backgrounds | `#34C48D` + `#EE9A5A` |
| `lileyka-mark-mono.svg` | 100×100 | print / single-colour | all `currentColor` (intentional mono exception) |
| `favicon.svg` | 64×64 | `<link rel="icon">` | white mark, `#F5C99B` centre on green tile `r=14` |
| `favicon-16.svg` | 16px | small favicon | same mark, heavier line for legibility |
| `lileyka-banner-1080x288.svg` | 1080×288 | README banner | centre `#F5C99B` for contrast on green |
| `lileyka-og-1200x630.svg` | 1200×630 | `og:image` | cream background, terracotta centre |
| `images/scouts-main.png` | — | welcome hero photo | not a brand mark |

`favicon.ico` (root of `public/`) is the unused Angular default and is not
referenced by `index.html`.

## In-app copies to keep in sync

The mark is inlined (not a shared component, to preserve the descendant CSS that
sizes and colours it). When the geometry changes, update **both**:

- `src/app/features/kurinModule/common/components/sidebar-menu/sidebar-menu.html`
- `src/app/features/systemModule/components/welcome-page/welcome-page.html`

The centre petal there carries `class="lil-mark-center"`, styled globally in
`src/lileyka-theme.css` (`stroke: var(--lil-clay-500)`), so it stays terracotta
across light/dark themes while the rest of the mark follows `currentColor`.
