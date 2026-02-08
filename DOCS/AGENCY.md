# Agency & Self-Direction (v0.4.0)

This document describes the design philosophy for v0.4.0. It defines how the system relates to the developer using it. It is a constraint document — it limits what the system may do, not what it must do.

## Core Principle

> v0.4.0 puts the developer in control of how and why they practice.

The system observes. The system offers. The system never directs.

---

## User-Declared Intent

Starting in v0.4.0, developers can optionally label their practice sessions with a declared intent:

| Intent | What the developer is saying |
|---|---|
| `Focus` | "I want to work on something specific." |
| `Challenge` | "I want to stretch myself." |
| `Maintenance` | "I want to stay in shape." |
| `Exploration` | "I want to see what's out there." |

### What declared intent does

- It is stored with the session record.
- It can be viewed in session history.
- It allows the user to reflect on patterns over time.

### What declared intent does NOT do

- It does not change scoring.
- It does not change difficulty.
- It does not change snippet selection.
- It does not change suggestions.
- It does not appear in any formula, weight, multiplier, or heuristic.
- It is never evaluated by the system.

Intent is the developer's language for their own practice. The system carries it but never acts on it.

---

## Practice Preferences

Users can configure:

| Preference | Purpose |
|---|---|
| Show Intent Chips | Whether the intent selector is visible. |
| Default Intent | An intent that is pre-selected on launch. |
| Practice Note | A free-text note for the user's own reference. |

### Design constraints

- Preferences never gate behavior. Hiding intent chips does not disable any feature.
- Default intent is a convenience, not a prescription. The user can change or clear it at any time.
- Practice notes are never read by the system. They are stored and displayed, nothing more.
- No preference is mandatory. The app works identically with all preferences at their defaults.

---

## The System's Relationship to the User

### The system supports, never manages

The system exists to provide:
- **Structure** — for developers who want it.
- **Observation** — for developers who want to reflect.
- **Silence** — for developers who want neither.

### The system observes, never judges

Observation means:
- Recording what happened.
- Computing factual summaries (averages, trends, frequencies).
- Presenting data without interpretation.

Judgment means (and is prohibited):
- Labeling sessions as "good" or "bad."
- Implying the user should practice more, differently, or better.
- Using urgency, guilt, or reward mechanics.

### The system offers, never insists

Suggestions from v0.3.0 (weakness practice, warmup, exploration) remain available. In v0.4.0:
- All suggestions use neutral language ("Consider", "Try", "Available").
- No suggestion uses imperative language ("You should", "You need to").
- No suggestion implies failure ("You haven't practiced in X days").
- Ignoring a suggestion has zero consequences.
- The system never remembers that a suggestion was ignored.

---

## What the System Explicitly Does Not Do

These are permanent constraints, not temporary omissions.

| The system does not... | Because... |
|---|---|
| Track streaks | Streaks create obligation. |
| Penalize inactivity | Returning should feel welcome. |
| Reward consistency | Consistency is the user's choice, not a game mechanic. |
| Compare to others | All practice is personal. |
| Display "improvement scores" | Scores imply targets. |
| Use "you should" language | Agency means no directives. |
| Escalate suggestions | Repetition implies the user is wrong. |
| Remember ignored suggestions | Ignoring is a valid choice. |
| Create urgency | Calm tools don't rush their users. |
| Gamify mastery | Mastery is felt, not awarded. |

---

## Backward Compatibility

v0.4.0 additions follow the same pattern as v0.3.0:

- **UserIntent** is nullable. Sessions without it are valid and common.
- **DeclaredIntent** on SessionRecord is nullable. v0.3.0 records have `DeclaredIntent = null`.
- **Practice preferences** default to sensible values. Missing fields deserialize safely.
- **No schema version bump required.** All new fields use nullable types and default initializers.
- **No behavior change.** All v0.3.0 workflows execute identically. Intent markers add data but never alter control flow.

---

## Philosophy

v0.3.0 introduced intentional practice — the system could observe patterns and suggest directions.

v0.4.0 ensures the developer stays in charge. The system's intelligence is available but never assertive. The developer chooses when to engage with structure and when to ignore it.

A developer who never selects an intent, never writes a practice note, and never follows a suggestion is using the app correctly.
