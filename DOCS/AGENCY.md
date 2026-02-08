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

## Automation Boundary (Phase 3)

Phase 3 introduced depth features: focus areas, pattern detection, and suggestion overrides. None of these automate decisions.

### Focus Area

- Focus Area is a user-set preference stored in `AppSettings.FocusArea`.
- It is written to `PracticeContext.Focus` as descriptive metadata.
- **No service reads `PracticeContext.Focus` for selection logic.** It is a label.
- The user can change or clear their focus area at any time. No cooldown, no penalty.

### Pattern Detection

- `PatternDetector.Detect()` returns `List<string>` — plain text observations.
- It never returns structured data, actions, or commands.
- It never triggers snippet selection, difficulty changes, or suggestions.
- Its output is displayed in StatsPanel and nowhere else.

### Suggestion Overrides

- Users can dismiss individual suggestions with a × button.
- Users can hide all suggestions via the Show Suggestions toggle.
- Dismissed suggestions are session-scoped — **never persisted across restarts**.
- The system never remembers, penalizes, or escalates based on dismissals.
- `ShowSuggestions = false` is a valid, permanent choice.

### Verification

Any future feature must satisfy:

1. **No service reads `DeclaredIntent`, `Focus`, or `ShowSuggestions` for scoring or selection.**
2. **No dismissed suggestion alters future behavior.**
3. **Pattern observations produce strings, never actions.**
4. **Turning off any v0.4.0 feature leaves the app functionally identical to v0.3.0.**

### v0.4.0 Release Audit (verified)

The following core services have **zero references** to any v0.4.0 feature
(`DeclaredIntent`, `UserIntent`, `FocusArea`, `ShowSuggestions`, `PracticeNote`,
`PatternDetector`, `TypistIdentityService`):

- `TypingEngine` — no v0.4.0 imports or references
- `SmartSnippetSelector` — no v0.4.0 imports or references
- `PracticeRecommender` — no v0.4.0 imports or references
- `AdaptiveDifficultyEngine` — no v0.4.0 imports or references
- `FatigueDetector` — no v0.4.0 imports or references
- `SessionPacer` — no v0.4.0 imports or references
- `WeaknessTracker` — no v0.4.0 imports or references
- `TrendAnalyzer` — no v0.4.0 imports or references

v0.4.0 features live exclusively in:
- `MainWindow.xaml.cs` (wiring only)
- `SettingsPanel` (UI controls)
- `StatsPanel` (display only)
- `TypingPanel` (intent chips, notes)
- `PatternDetector` (string-only output)
- `TypistIdentityService` (display-only output)
- `PersistenceService` (sanitization guards)

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

v0.5.0 values continuity over novelty. The app should feel trustworthy over months or years. Returning after a long break should feel natural. Progress is understood as uneven but valid. Nothing pressures the user to "perform."

A developer who never selects an intent, never writes a practice note, and never follows a suggestion is using the app correctly.

---

## Continuity Principles (v0.5.0)

v0.5.0 is about the app aging well with the user.

### Core Beliefs

| Belief | Implication |
|---|---|
| Long gaps are normal | No penalty, no "welcome back" fanfare, no catch-up pressure |
| Updates should be invisible | History, identity, and preferences survive version transitions |
| Plateaus are valid | Flat progress is not a problem to solve |
| Regression happens | Temporary dips do not dominate the narrative |
| Familiarity beats novelty | Predictable behavior is more valuable than new features |
| Personalization is gentle | The app adapts slowly and reversibly |

### What the System Explicitly Does Not Do (v0.5.0 additions)

| The system does not... | Because... |
|---|---|
| Show "welcome back" banners | Returning is normal, not an event |
| Highlight what changed between versions | Updates should feel continuous |
| Alert about regression | Temporary dips are not emergencies |
| Accelerate suggestions after long gaps | Urgency undermines trust |
| Reset personalization on update | Stability means your app stays yours |
| Over-explain new features | The app should feel familiar, not tutorial-heavy |

### Longevity Contract

The user's data is theirs. Across any number of version updates:

1. **Session history is never discarded.** Records persist within their cap (500).
2. **Identity is computed from existing data.** No new persisted fields required for identity.
3. **Preferences are additive.** New settings default to sensible values. Existing settings are preserved.
4. **Behavior is stable.** Core workflows (start, type, complete) never change feel.
5. **Adaptation is reversible.** Any personalization the system applies can be undone or frozen.
