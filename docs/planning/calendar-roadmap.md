# Calendar roadmap — recurrence, comments, reactions, external sync

Builds on the v0.18.0 agenda. Current surface:

- Entities: `ProjectK.Common/Entities/KurinModule/Agenda/AgendaEntities.cs`
  (`AgendaItem`, `AgendaAssignment`).
- Handlers: `ProjectK.BusinessLogic/Modules/KurinModule/Features/Agenda/*`.
- Repo: `ProjectK.Infrastructure/Repositories/AgendaItemRepository.cs`
  (`GetForViewerAsync` = date-window + assignment-scope filter).
- Frontend: `features/kurinModule/agenda-calendar`, `agenda-board`,
  `common/components/agenda-item-dialog`, `common/services/agenda-service`,
  `common/models/agenda.ts`.
- `AgendaItem` already stores full UTC (`StartUtc`/`EndUtc`, `IsAllDay`) → hourly
  view needs no time-of-day migration. **No recurrence exists today.**

## Phase 1 — Recurrence («репітінг»)

**Model.** Add to `AgendaItem`: `RecurrenceRule` (RRULE string, RFC 5545 subset),
`RecurrenceEndUtc?`, and a self-referential `SeriesKey?` (parent item). Add an
`AgendaOccurrenceException` table for edited/cancelled single occurrences
(`SeriesKey`, `OccurrenceStartUtc`, `IsCancelled`, override fields). New EF
migration.

**Expansion — materialize on read.** In `GetForViewerAsync`, after loading series
rows overlapping the window, expand RRULE into occurrences **within the requested
`fromUtc..toUtc`** window only (a bounded expansion; never unbounded). Apply
exceptions. This keeps storage light and matches the calendar's windowed fetch.
Board view (`GetAgendaBoard`) shows the next N task occurrences.

**Recommended library.** `Ical.Net` for RRULE parsing/expansion (also reused for
the `.ics` feed in Phase 4). Validate the rule in `CreateAgendaItemValidator`.

**Frontend.** Extend `agenda-item-dialog` with a repeat control (none / daily /
weekly-by-day / monthly + until), serialized to RRULE in `agenda.ts`. Editing a
recurring item asks "this occurrence / whole series".

**Effort:** M–L (backend expansion + exceptions is the bulk).

## Phase 2 — Comments

**Model.** New `AgendaComment` (`AgendaCommentKey`, `AgendaItemKey`,
`AuthorUserKey`, `Body`, `CreatedUtc`, `EditedUtc?`, `DeletedUtc?`). Repo +
handlers (Create/Update/Delete/List) mirroring the agenda feature; endpoints under
`AgendaController` (`GET/POST {key}/comments`, `PUT/DELETE {key}/comments/{id}`),
guarded by the existing `[ResourceAuthorize(ResourceType.Kurin, …)]` and viewer
scope.

**Notifications.** Add `AppNotificationType.AgendaItemCommented`; reuse
`NotificationService` + `AgendaNotificationRecipients` to notify the creator and
assignees (dedup by item).

**Frontend.** A comment thread in `agenda-item-dialog`; author names via the
existing `AgendaCreatorNames` pattern.

**Effort:** M.

## Phase 3 — Reactions

**Model.** New `AgendaReaction` (`TargetType` = Item|Comment, `TargetKey`,
`UserKey`, `Emoji`/`ReactionType`); unique index `(TargetType, TargetKey,
UserKey, Emoji)`. Toggle endpoint; return aggregated counts + the viewer's own
reactions on the item/comment response models.

**Frontend.** Optimistic toggle (same pattern as the board's status drag rollback).

**Effort:** S–M.

## Phase 4 — External calendars

### 4a. iCal subscription (recommended first — best value/effort)
Per-viewer read-only `.ics` feed at `GET /calendar/{kurinKey}/feed.ics?token=…`
built with `Ical.Net` from the viewer's visible agenda (reusing
`GetForViewerAsync` + RRULE). Auth via a signed, revocable per-user token (not the
session cookie) so it works in Google/Apple/Outlook/Notion "subscribe by URL".
**One-way, no OAuth, no secret storage.**
**Effort:** S–M.

### 4b. Google Calendar two-way sync (later)
Google OAuth, Calendar API, a `AgendaExternalLink` mapping table
(item ↔ external event id), delta sync + conflict rules, refresh-token storage.
Requires **Key Vault** (none today — see Azure audit) for client secret + tokens.
**Effort:** L. Gate behind demand.

### 4c. Notion
Notion has no calendar-sync API. Two realistic options: (i) subscribe to the 4a
`.ics` from a Notion calendar view (zero extra work once 4a ships), or (ii)
one-way push agenda items into a Notion database via the Notion API. Recommend
(i); document (ii) as optional.

## Cross-cutting

- **Realtime:** notifications are pull-based (no SignalR). Live comment/reaction
  updates would need SignalR or polling — decide per phase; polling on dialog
  open is enough for v1.
- **Ordering:** Recurrence (1) first — it's the user's headline ask and unblocks
  the `.ics` feed (4a). Comments (2) + reactions (3) can share primitives with the
  feed work (see `feed-and-dashboard.md`).
