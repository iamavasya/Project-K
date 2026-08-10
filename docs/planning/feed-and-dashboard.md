# Feed / post page + role dashboard — research & tech plan

Two candidate features. The feed is explicitly **"decide at planning stage, may
drop"**; the dashboard is low-risk and reuses existing pieces.

## A. Kurin feed / post page

### What exists to build on
- **No** feed/post/comment/reaction primitive exists. Nearest is admin
  `PublicAnnouncements` (→ Telegram) — not a per-kurin social feed.
- Scoping pattern: `AgendaViewerScope` + `ResourceAccessService` (kurin → gurtok →
  member visibility) — reuse verbatim.
- Notifications: `NotificationService` (DB-write, pull-based).
- Media: `AzureBlobPhotoService` — **single image per entity**, compressed to JPEG
  q75 ≤1920px, `Cache-Control: 1y`, orphan-cleaned by `OrphanPhotoCleanupService`,
  in one **public** `photos` container.

### Proposed model
- `Post` (`PostKey`, `KurinKey`, scope target like agenda, `AuthorUserKey`,
  `Body`, `CreatedUtc`, `EditedUtc?`, `DeletedUtc?`).
- `PostAttachment` (`PostKey`, `BlobName`, `ContentType`, `SizeBytes`, `Width`,
  `Height`, `OrderIndex`) — the **new multi-attachment** model a feed needs.
- Comments & reactions: **share the agenda primitives** from
  `calendar-roadmap.md` (generalize `TargetType` to include Post) rather than
  duplicating.
- Notifications: `AppNotificationType.PostPublished` / `PostCommented`.

### Media & resource cost (this is the deciding factor)

Reuse server-side compression (JPEG q75 ≤1920px). Rough per-image ≈ **0.25 MB**.

| Scenario | posts/mo | imgs/post | new GB/mo | storage $/mo* | + after 12 mo |
|---|---|---|---|---|---|
| Light | 40 | 1 | ~0.01 | ~$0.0002 | ~0.12 GB |
| Medium | 150 | 3 | ~0.11 | ~$0.002 | ~1.3 GB |
| Heavy | 400 | 5 | ~0.5 | ~$0.009 | ~6 GB |

\* Blob Hot LRS ≈ $0.018/GB-month. **Storage itself is negligible**; the real
cost driver is **egress** (≈$0.087/GB after the first 100 GB/mo free) on
frequently-viewed public images. Mitigations: keep compression, cap attachments
(e.g. ≤5/post, ≤5 MB each), images-only in v1 (defer arbitrary files/PDF), long
cache headers (already set), and **front the container with a CDN** (also a
security win — see Azure audit's public-container finding). Extend
`OrphanPhotoCleanupService` to cover `PostAttachment`.

**Recommendation:** ship a **text-only v1** (zero new storage cost) behind the
existing scope/notifications, then add attachments in v2 **after** the CDN /
public-access fix from the Azure audit. This de-risks both cost and the public
`photos` exposure.

### Effort
Text-only v1: M. Attachments v2: M (model + upload + cleanup + CDN).

## B. Role dashboard

### What exists to build on
- **Tile board** `src/app/shared/tile-board/*` (`tile-board.component.ts`,
  `tile-layout.service.ts`, `tile-def.directive.ts`, `tile-order.function.ts`) —
  reorderable, already used by `member-card`.
- Persistence: `UserTileLayout` (`UserKey`, `BoardKey`, `TileOrderJson`,
  `SchemaVersion`) + `SaveTileLayoutCommand`/`ResetTileLayoutCommand`/
  `GetTileLayoutsQuery`. **No new storage model needed.**
- Gating: `permission.service.ts` (`canManageAgenda`, `canReviewSkills`,
  `canManagePlanning` …) + roles Admin/Manager/Mentor/User + leadership
  `Kurinnuy`/`Hurtkoviy`.
- Data sources already available: agenda (calendar/board), probes & badges
  («проба»/«вмілості») in `ProbesAndBadgesModule`, member/kurin stats, notifications.

### Proposed tiles × role

| Tile | Data source | User | Mentor | Гуртковий | Курінний | Manager/Admin |
|---|---|---|---|---|---|---|
| My agenda (next events/tasks) | agenda `GetForViewer` | ✅ | ✅ | ✅ | ✅ | ✅ |
| My probe/skill progress | ProbesAndBadges member-progress | ✅ | ✅ | ✅ | ✅ | — |
| Reviews pending («вмілості») | skills-review | — | ✅ | ✅ | ✅ | ✅ |
| Gurtok roster/progress | members + progress (scoped) | — | ✅ | ✅ | ✅ | ✅ |
| Kurin overview stats | kurin aggregates | — | — | — | ✅ | ✅ |
| Unread notifications | NotificationService | ✅ | ✅ | ✅ | ✅ | ✅ |

Implement as a new `dashboard` route composing `tile-board` with a
`BoardKey="dashboard-<role>"`; each tile is a small standalone component gated by
`permission.service`. Reuse existing queries — **no new backend endpoints** for a
v1 that surfaces already-exposed data.

### Effort
S–M (mostly frontend composition; server reuse).

## Verdict for the planning gate
- **Dashboard:** proceed — cheap, reuses tile-board + existing data, clear role value.
- **Feed:** proceed **text-only first**; hold media attachments until the Azure
  public-container/CDN fix lands, then reassess egress cost.
