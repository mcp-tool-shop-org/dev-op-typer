# Future Features — v2 Ideas

Features that were removed, deferred, or imagined during development.
None of these are committed — they exist so good ideas don't get buried.

---

## Removed (Could Return)

### User-Declared Intent (v0.4.0)
Intent chips (Focus, Challenge, Maintenance, Exploration) let users label sessions.
Intent had zero impact on scoring, difficulty, or selection — purely metadata.
Removed because it was UI clutter with no behavioral consequence.

**If restored:** Make intent _mean_ something. A "Focus" intent could auto-select
from top weakness categories. A "Challenge" could push +1 difficulty above
comfort zone. The infrastructure is still in the codebase (UserIntent enum,
PracticeIntent enum, DeclaredIntent on SessionRecord, PersistenceService
sanitization). Intent-aware insights in InsightEngine still reference it.

### Intent Pattern Stats
StatsPanel "By Intent" section showing factual averages grouped by declared intent.
Removed alongside intent UI.

**If restored:** Only meaningful if intent labels carry behavioral weight.

### Show Intent Chips / Default Intent Settings
Settings toggles for showing/hiding intent chips and pre-selecting a default.
Removed alongside intent UI. AppSettings properties still exist for serialization
backward compat.

---

## Deferred (Infrastructure Ready)

### SignalPolicy Expansion
Two reserved flags exist in SignalPolicy:
- `SignalsAffectDifficulty` — weakness signals could influence difficulty band selection
- `SignalsAffectXP` — weakness signals could modify XP multiplier

Currently both are `false` with no UI exposure. The flag architecture is in place.

### Calibration Stats Exclusion
`CalibrationTag.IsCalibration()` exists but calibration sessions are still
included in lifetime stats. Could filter them out for "pure" practice metrics.

### Additional Calibration Languages
Calibration snippets exist across 5 languages (python, rust, javascript,
csharp, go). The architecture supports any language — just add JSON packs.

---

## Imagined (No Infrastructure Yet)

### Timed Sessions
Fixed-duration practice (1min, 3min, 5min) instead of per-snippet sessions.
Would need a timer overlay and mid-snippet completion handling.

### Snippet Playlists
Curated sequences of snippets for focused practice on a specific topic.
Could build on existing practice profiles.

### Multi-Language Sessions
Practice that switches languages mid-session. Would require cross-language
rating normalization and a unified snippet queue.

### Typing Replay
Record keystroke timings and replay sessions to visualize hesitation points.
The per-character heatmap already tracks error positions — replay would add
the temporal dimension.

### Custom Themes
User-defined color palettes beyond the built-in dark theme. The brush
resolution system already supports theme resources — would need a theme
editor and persistence.

### Export Statistics
Export session history, weakness data, and trend graphs as CSV or PDF
for personal tracking outside the app.

### Snippet Difficulty Preview
Before starting a snippet, show estimated difficulty based on symbol density,
line count, and indent depth. The DifficultyDeriver already computes this —
just needs UI exposure.

---

## Permanent Non-Goals

These boundaries protect what the tool is. They are load-bearing.

- **No cloud, no accounts** — everything local
- **No social features** — no leaderboards, multiplayer, comparison
- **No gamification pressure** — no streaks, daily goals, FOMO
- **No AI code generation** — snippets are hand-authored
- **No plugin system** — extensions are data (JSON), never code
- **No IDE integration** — standalone tool
- **No lesson plans or curricula** — adaptive difficulty is the curriculum
- **No real-time typing analysis** — no finger tracking, no ergonomic advice
