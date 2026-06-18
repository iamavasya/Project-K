# Notification Events

This document tracks notification events for the persistent in-app notification system.

## Current Rules

- Notifications are persisted in `AppNotifications` and shown through the frontend bell/inbox.
- Events must be emitted only after the domain change was saved successfully.
- Do not use fire-and-forget. Await `INotificationService`.
- Use `DeduplicationKey` for repeated unread events that should update an existing notification instead of creating noise.
- `PayloadJson` should remain small and optional. The main navigation target should be `Route`.
- User-facing notifications should be skipped when the target `Member` is not linked to an app user through `UserKey`.

## Implemented Events

| Type | Trigger | Recipients | Severity | Route | Deduplication |
| --- | --- | --- | --- | --- | --- |
| `MemberProfileVerified` | A mentor, manager, or admin verifies a member profile. | Linked owner of the member profile. | `Success` | `/member/{memberKey}` | `member-profile-verified:{memberKey}` |
| `MemberProfileChangedAfterVerification` | A verified-current member profile is changed and becomes verified-stale. | Linked owner of the member profile. | `Warn` | `/member/{memberKey}` | `member-profile-stale:{memberKey}` |
| `MemberSkillSubmittedForReview` | A member submits or resubmits a badge/skill for review. | Managers of the member's kurin and active mentors assigned to the member's group, excluding the actor. | `Info` | `/kurin/{kurinKey}/review/skills` | `skill-review:{memberKey}:{badgeId}` |
| `MemberSkillReviewed` | A submitted badge/skill is approved, rejected, or removed after confirmation. | Linked owner of the member profile. | `Success` when approved, `Warn` when rejected or removed. | `/member/{memberKey}` | `skill-review-result:{memberKey}:{badgeId}` |
| `MemberAwardSubmitted` | A member award is created or resubmitted for review. | Managers of the member's kurin and active mentors assigned to the member's group, excluding the actor. | `Info` | `/member/{memberKey}` | `award-submitted:{memberAwardKey}` |
| `MemberAwardReviewed` | A submitted member award is approved or rejected. | Linked owner of the member profile. | `Success` when approved, `Warn` when rejected. | `/member/{memberKey}` | `award-review:{memberAwardKey}` |
| `MemberWarningAssigned` | A warning level is assigned to a member. | Linked owner of the member profile. | `Warn` | `/member/{memberKey}` | `member-warning:{memberWarningKey}` |

## Reserved But Not Yet Emitted

| Type | Intended Meaning | Status |
| --- | --- | --- |
| `LeadershipChanged` | Kurin or group leadership changed. | Enum exists, event emission not implemented. |

## Planned High-Priority Events

### 1. Warning Cancelled

Type:
- Add a new enum value, for example `MemberWarningCancelled`.

Trigger:
- `CancelMemberWarning` revokes an active warning.

Recipients:
- Linked owner of the member profile.

Suggested severity:
- `Success` or `Info`

Suggested route:
- `/member/{memberKey}`

Suggested deduplication:
- `member-warning-cancelled:{memberWarningKey}`

Notes:
- This pairs with `MemberWarningAssigned` and makes warning lifecycle visible to the affected member.

### 2. Leadership Changed

Type: `LeadershipChanged`

Trigger:
- Leadership assignment is created, changed, ended, or transferred.

Recipients:
- Newly assigned leader if linked to a user.
- Previous leader when a leadership term is ended or transferred.
- Manager of the kurin for group-level leadership changes, if not the actor.

Suggested severity:
- `Info`

Suggested route:
- Kurin-level leadership: `/kurin`
- Member-specific context: `/member/{memberKey}`

Suggested deduplication:
- `leadership-changed:{leadershipKey}:{memberKey}`

Notes:
- Keep this scoped. Do not notify every kurin member for each leadership adjustment in beta.

### 3. Profile Verification Reset By Reviewer

Type:
- Add a new enum value, for example `MemberProfileVerificationReset`.

Trigger:
- `ResetMemberProfileVerification` manually clears profile verification.

Recipients:
- Linked owner of the member profile.

Suggested severity:
- `Warn`

Suggested route:
- `/member/{memberKey}`

Suggested deduplication:
- `member-profile-verification-reset:{memberKey}`

Notes:
- This is distinct from stale verification. Stale means data changed; reset means a reviewer explicitly removed verification.

## Recommended Implementation Order

1. Add `MemberWarningCancelled` to complete warning lifecycle notifications.
2. Add `LeadershipChanged` with a narrow recipient list.
3. Add `MemberProfileVerificationReset` if product wants manual reset to be visible to members.

## Open Design Questions

- Should managers receive all review notifications by default, or only if they opt into review work?
- Should mentors receive award review notifications, or only skill review notifications?
- Should notification events use localized titles/bodies at creation time, or should frontend derive text from type and payload later?
- Should a dedicated review inbox page replace routing reviewers to existing skill/member pages?
