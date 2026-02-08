# Practice Concepts (v0.3.0)

This document describes what the DevOpTyper system can represent for deliberate practice. It defines **capabilities**, not prescriptions. Nothing here is mandatory.

## Core Principle

> v0.3.0 helps developers practice typing code deliberately over time.

If a concept doesn't support that sentence, it doesn't belong here.

---

## Session Context

Every typing session can carry optional metadata describing **why** it happened.

### Intent

A session may have an intent:

| Intent | Meaning |
|---|---|
| `Freeform` | No particular reason. Just practicing. |
| `WeaknessTarget` | Targeting a specific weak character, group, or topic. |
| `Repeat` | Repeating a previously completed snippet. |
| `Exploration` | Trying a new language or difficulty level. |
| `Warmup` | Short, easy practice to get started. |

Intent is always optional. A session with no intent is perfectly valid.

### Focus

A session may focus on something specific:
- A character group (e.g., `"brackets"`)
- A language topic (e.g., `"python:list_comprehensions"`)
- A weakness (e.g., the character `{`)

Focus is a free-form string. The system does not validate or enforce it.

### Group Tag

Sessions may share a group tag for loose association. Group tags are strings — there is no enforced hierarchy, sequence, or structure.

Example: three sessions tagged `"morning-warmup"` are related by the user's intent, not by the system's rules.

---

## Longitudinal Data

The system accumulates data across sessions without interpreting it.

### Language Trends

For each language, the system stores:
- **Recent WPM values** (last 50 sessions)
- **Recent accuracy values** (last 50 sessions)
- **Session count** and **first/last session dates**

These are raw numbers. The system computes averages but does not judge them.

### Session Cadence

The system records when sessions happen (timestamps only). This enables:
- Detecting practice frequency without enforcing it
- Identifying gaps without shaming
- Observing fatigue patterns without blocking

### Weakness Snapshots

Once per day per language, the system captures a snapshot of the user's weakest characters. Over time, these snapshots reveal:
- Which weaknesses persist
- Which weaknesses resolve
- Whether the user's weak spots are changing

Snapshots are observations, not evaluations.

---

## What the System Deliberately Does Not Do

- **No daily streaks.** The system records when you practice. It does not reward consistency or punish gaps.
- **No mandatory structure.** Practice modes, focuses, and intents exist for users who want them. They are never required.
- **No scoring of trends.** The system stores data points. It does not compute "improvement scores" or "regression alerts."
- **No social comparisons.** All data is local. There are no leaderboards, rankings, or peer comparisons.
- **No cloud sync.** All data lives on the user's machine. Nothing leaves the device.

---

## Data Lifecycle

| Data | Retention | Cap |
|---|---|---|
| Session records | Indefinite | 500 most recent |
| Session timestamps | Indefinite | 200 most recent |
| Language trend points | Indefinite | 50 per language |
| Weakness snapshots | Indefinite | 90 most recent |
| Practice context | Per-session | Stored with record |

All data can be exported as JSON. All data can be reset.

---

## Backward Compatibility

v0.3.0 data additions are designed to coexist with v0.2.0 persisted state:

- **PracticeContext** is nullable on SessionRecord. v0.2.0 sessions have `Context = null`.
- **LongitudinalData** defaults to empty collections. Missing fields deserialize safely via `System.Text.Json` defaults.
- **No schema version bump required.** All new fields use default initializers (`new()`, `null`).
- **SanitizeBlob** null-checks all new fields on load, ensuring corrupt or partial data never crashes the app.
- **No behavior change.** Existing workflows (start, type, complete, next snippet) execute identically with or without v0.3.0 data.

---

## Philosophy

The system exists to **support** deliberate practice, not to **manage** it.

A developer who opens the app, types one snippet, and closes it had a valid session. A developer who practices for two hours with structured focus had a valid session. The system treats both equally.

Randomness remains valid. Structure exists for those who want it.
