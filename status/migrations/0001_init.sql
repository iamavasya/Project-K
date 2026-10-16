-- Checks are not stored one row each: the Free plan allows 100k written rows a day for the
-- whole account (shared with the catalog), so every check updates one hour row and one day row.

-- The latest check per target, and since when it has been in that state.
CREATE TABLE target_state (
  target      TEXT PRIMARY KEY,
  state       TEXT NOT NULL CHECK (state IN ('up', 'degraded', 'down')),
  since       INTEGER NOT NULL,
  checked_at  INTEGER NOT NULL,
  latency_ms  INTEGER,
  version     TEXT,
  detail      TEXT
) WITHOUT ROWID;

-- hour = epoch hours; kept for two days, enough for the 24-hour response-time strip.
CREATE TABLE target_hours (
  target      TEXT NOT NULL,
  hour        INTEGER NOT NULL,
  checks      INTEGER NOT NULL DEFAULT 0,
  degraded    INTEGER NOT NULL DEFAULT 0,
  down        INTEGER NOT NULL DEFAULT 0,
  latency_sum INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (target, hour)
) WITHOUT ROWID;

-- day = 'YYYY-MM-DD' in UTC; the 90-day bars.
CREATE TABLE target_days (
  target      TEXT NOT NULL,
  day         TEXT NOT NULL,
  checks      INTEGER NOT NULL DEFAULT 0,
  degraded    INTEGER NOT NULL DEFAULT 0,
  down        INTEGER NOT NULL DEFAULT 0,
  latency_sum INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (target, day)
) WITHOUT ROWID;

-- One row per state change: the automatic history under the bars.
CREATE TABLE target_events (
  id          INTEGER PRIMARY KEY,
  target      TEXT NOT NULL,
  at          INTEGER NOT NULL,
  state       TEXT NOT NULL,
  previous    TEXT,
  detail      TEXT
);
CREATE INDEX target_events_at ON target_events (at);

-- Written by hand in /admin: incidents, planned maintenance, notices.
CREATE TABLE incidents (
  id          INTEGER PRIMARY KEY,
  kind        TEXT NOT NULL CHECK (kind IN ('incident', 'maintenance', 'notice')),
  title       TEXT NOT NULL,
  body        TEXT NOT NULL DEFAULT '',
  targets     TEXT NOT NULL DEFAULT '',
  starts_at   INTEGER NOT NULL,
  resolved_at INTEGER,
  created_by  TEXT NOT NULL,
  updated_at  INTEGER NOT NULL
);

-- Anonymous telemetry: the latest ping per instance...
CREATE TABLE instances (
  id              TEXT PRIMARY KEY,
  first_seen      INTEGER NOT NULL,
  last_seen       INTEGER NOT NULL,
  version         TEXT NOT NULL,
  kind            TEXT NOT NULL,
  country         TEXT,
  krais           TEXT NOT NULL DEFAULT '',
  kurins          INTEGER NOT NULL DEFAULT 0,
  members         INTEGER NOT NULL DEFAULT 0,
  active_7d       INTEGER NOT NULL DEFAULT 0,
  active_30d      INTEGER NOT NULL DEFAULT 0,
  requests_24h    INTEGER NOT NULL DEFAULT 0,
  errors_24h      INTEGER NOT NULL DEFAULT 0,
  uptime_hours    INTEGER NOT NULL DEFAULT 0,
  silent_alerted  INTEGER NOT NULL DEFAULT 0
) WITHOUT ROWID;

-- ...and one row per instance per day for the trends. A second ping that day overwrites it.
CREATE TABLE instance_days (
  instance     TEXT NOT NULL,
  day          TEXT NOT NULL,
  version      TEXT NOT NULL,
  kind         TEXT NOT NULL,
  members      INTEGER NOT NULL DEFAULT 0,
  active_7d    INTEGER NOT NULL DEFAULT 0,
  requests_24h INTEGER NOT NULL DEFAULT 0,
  errors_24h   INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (instance, day)
) WITHOUT ROWID;
CREATE INDEX instance_days_day ON instance_days (day);

-- Small key/value: the latest release, today's count of new instances.
CREATE TABLE meta (
  key   TEXT PRIMARY KEY,
  value TEXT NOT NULL
) WITHOUT ROWID;
