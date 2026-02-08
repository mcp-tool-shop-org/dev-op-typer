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

### What "Knowing the User" Means (v0.5.0)

The system builds familiarity with the user over time. This is not profiling — it is pattern recognition applied to make the tool quieter and more accurate.

| What the system learns | How it uses it |
|---|---|
| Natural WPM variability (CV) | Calibrates trend thresholds — variable typists need wider bands to register real change |
| Session burst patterns | Surfaces as factual observation ("~3 sessions at a time"), never as prescription |
| Day-of-week preferences | Surfaced as pattern ("Practices more on weekdays"), never used for scheduling |
| Personal WPM range (P10–P90) | Shown in identity section, never used for difficulty or scoring |
| Milestone thresholds reached | Shown as facts ("100 sessions"), not celebrations |

#### What "knowing" does NOT mean

- The system never predicts what the user will do next.
- The system never schedules, reminds, or nudges based on patterns.
- The system never compares the user's rhythm to any other user.
- The system never labels a rhythm as "healthy", "productive", or "optimal".
- Pattern observations produce strings, never actions (same constraint as v0.4.0).

---

## Extensibility Philosophy (v0.6.0)

v0.6.0 lets advanced users shape how they practice without the tool getting in the way. This is about authorship, not platforms.

### Core Principle

> Extensions are augmentations, not overrides. Core guarantees are non-negotiable.

### What May Be Extended

| Area | What users can do | Mechanism |
|---|---|---|
| Practice material | Create snippet files in any language or domain | JSON files in `UserSnippets/` |
| Practice configurations | Tune difficulty bias, warmup behavior, session structure | JSON files in `UserConfigs/` |
| Organization | Group and label their material however they want | Directory structure + metadata |

### What May Never Be Extended

| Area | Why it's frozen |
|---|---|
| Typing engine | Correctness of keystroke processing is a trust guarantee |
| XP formula | Consistency across all content prevents gaming |
| Data persistence | User history is sacred — extensions cannot corrupt it |
| Session records | Built-in and user content produce identical records |
| Identity computation | Longitudinal self-portrait must reflect the whole picture |

### Design Constraints

1. **No publishing.** User content is local. There is no account model, no upload, no sharing platform.
2. **No ratings.** User snippets are not rated, ranked, or compared to built-in content.
3. **No lock-in.** User content is plain JSON. It can be edited in any text editor, copied, or deleted.
4. **No overhead.** If no extensions exist, the app behaves identically to v0.5.0. Zero scanning, zero cost.
5. **No second-tier experience.** User-authored content behaves exactly like built-in content during practice.
6. **No network.** Extensions never introduce network calls, telemetry, or external dependencies.

### Extension Safety

Every user-provided file is validated before loading:

- Snippet count per file is capped (200)
- Code length per snippet is capped (5000 chars)
- Required fields (id, code) are enforced
- Difficulty must be 1-5
- Malformed files are skipped silently — they never crash the app
- Total user snippet files capped at 50

### Authorship Without Gamification

User content creation is a personal act, not a social one. The system treats authored material with the same respect it gives to the user's practice data.

| The system does not... | Because... |
|---|---|
| Rate user snippets | Quality judgment belongs to the author |
| Count how often user snippets are practiced | Usage is private, not a metric |
| Rank user content against built-in content | There is no hierarchy of material |
| Suggest "popular" or "trending" snippets | There is no audience |
| Track snippet "completion" or "mastery" | Material is for practice, not collection |
| Encourage creating more content | Creation is voluntary, not a quota |
| Display "authored by you" badges | The user knows what they wrote |
| Provide snippet templates or wizards | A text editor and the JSON schema are enough |

#### What authorship looks like

- The user creates a JSON file in any text editor.
- The user drops it into `UserSnippets/` (or a subfolder).
- The app loads it next time it starts.
- The snippet appears in the normal rotation.
- Practice, scoring, XP, and session records are identical to built-in content.
- The user can edit, move, or delete the file at any time.
- The app never modifies, annotates, or locks user files.

#### What the system provides

- A status line showing how many snippets were loaded.
- Error messages if files are malformed (never blocks the app).
- A button to open the snippets folder.
- That's it.

### Verification

Any future extension feature must satisfy:

1. **Removing all user content leaves the app functionally identical to v0.5.0.**
2. **No user content alters scoring, XP, or accuracy computation.**
3. **No configuration overrides a frozen behavior.**
4. **All user artifacts are human-readable JSON.**
5. **The app never scans for or suggests extensions the user hasn't created.**
6. **No gamification mechanics apply to authorship — no streaks, badges, levels, or achievements for creating content.**
7. **Portable bundles are standard ZIP files containing plain JSON — no proprietary format.**

---

## Portable Bundle Format

User content can be exported as a ZIP bundle and imported on any machine. The format is deliberately simple and open.

### Bundle structure

```
bundle.zip/
  manifest.json        — metadata (version, timestamp, counts)
  snippets/            — user snippet files
    python.json
    my-favorites.json
    terraform/
      aws.json
  configs/             — user practice configs
    morning-warmup.json
    strict-mode.json
```

### Snippet file schema

Each snippet file is a JSON array of snippet objects:

```json
[
  {
    "id": "unique-id",
    "language": "python",
    "difficulty": 3,
    "title": "List comprehension",
    "code": "squares = [x**2 for x in range(10)]",
    "topics": ["comprehensions", "lists"],
    "explain": ["Builds a list of squares from 0 to 81"]
  }
]
```

Required fields: `id`, `code`.
Optional fields: `language` (defaults to filename), `difficulty` (defaults to 1), `title`, `topics`, `explain`.

### Config file schema

Each config file is a single JSON object:

```json
{
  "description": "Start easy, loosen whitespace rules",
  "difficultyBias": "easier",
  "warmup": true,
  "preferShorterSnippets": true,
  "whitespace": "lenient"
}
```

All fields are optional. Missing fields inherit from app defaults.

Valid `difficultyBias` values: `"easier"`, `"harder"`, `"match"`.
Valid `whitespace` values: `"strict"`, `"lenient"`, `"normalize"`.
Valid `backspace` values: `"always"`, `"limited"`, `"never"`.
`accuracyFloor`: number, 0-100.

### manifest.json

```json
{
  "appVersion": "0.6.0",
  "exportedAt": "2025-01-15T10:30:00Z",
  "snippetFileCount": 3,
  "configFileCount": 2
}
```

The manifest is informational only. Bundles without a manifest are still importable.

### Design guarantees

- **Standard ZIP**: No proprietary headers, encryption, or compression schemes.
- **Plain JSON**: Readable and editable with any text editor.
- **No lock-in**: Files work outside the app. The schema is documented here.
- **Non-destructive import**: Existing files are never overwritten.
- **Path safety**: Import sanitizes paths and rejects directory traversal attempts.
- **No network**: Export and import are local operations. No upload, no download, no accounts.

---

## Community Philosophy (v0.7.0)

v0.7.0 explores what community means for a practice tool that rejects social mechanics.

### Core Principle

> Community is shared craft, not shared identity.

Developers can benefit from each other's practice material, explanations, and collective observations without knowing who created them, without profiles, and without any pressure to participate.

### What Community Means

| Aspect | How it works |
|---|---|
| Shared artifacts | Snippets, configs, and explanations can be exchanged as portable bundles |
| Multiple perspectives | Explanations may offer different viewpoints on the same snippet |
| Collective signals | Anonymized aggregate observations about snippet difficulty or common errors |
| Optional participation | Every community feature is opt-in and reversible |

### What Community Does NOT Mean

These are permanent constraints, not temporary omissions.

| The system does not... | Because... |
|---|---|
| Create accounts or profiles | Identity has no place in personal practice |
| Track who authored what | Attribution creates hierarchy |
| Display download counts or ratings | Popularity metrics distort value |
| Rank community content against built-in | There is no hierarchy of material |
| Show feeds, timelines, or notifications | Social mechanics create obligation |
| Enable followers, likes, or comments | Approval-seeking undermines practice |
| Encourage sharing as a goal | Sharing is utilitarian, not expressive |
| Compare users to each other | All practice is personal |
| Phone home or collect usage data | The app is local-first, always |

### Community Content Location

Community content lives at `%LOCALAPPDATA%/DevOpTyper/CommunityContent/`, separate from user-authored content in `UserSnippets/` and `UserConfigs/`. The filesystem separation is organizational only — at runtime, community content behaves identically to user-authored and built-in content.

### Verification

Any community feature must satisfy:

1. **Removing all community content leaves the app functionally identical to v0.6.0.**
2. **No community content alters scoring, XP, accuracy, or difficulty.**
3. **No origin tracking persists at runtime.** Content source is indistinguishable during practice.
4. **All community artifacts are human-readable JSON.**
5. **No community feature introduces network calls, telemetry, or external dependencies.**
6. **Solo practice remains complete and satisfying without any community content.**
7. **Nothing pressures users to participate, share, or engage socially.**
8. **If CommunityContent/ does not exist, zero scanning occurs.** No overhead, no prompts, no empty-state messaging.
9. **Community content never appears in any "count" or "status" unless the user has explicitly imported it.**

---

## Sharing Without Social Signaling

Sharing in Dev-Op-Typer is a file operation, not a social act.

### What sharing looks like

- A user exports their snippets and configs as a ZIP file.
- They send it to a colleague, post it in a forum, or host it on a file share.
- The recipient imports it — choosing whether it goes to "My Content" or "Community Content."
- The imported material appears in the normal practice rotation.
- No one tracks who shared what, how often it was imported, or whether it was useful.

### What sharing does NOT look like

| Sharing does not involve... | Because... |
|---|---|
| Origin tracking | Who created something is irrelevant to practice |
| Download counts | Popularity metrics distort perceived value |
| "Imported from X" badges | Attribution creates hierarchy |
| Author profiles or pages | Sharing is about content, not identity |
| Ratings or reviews | Quality judgment belongs to the user |
| Version tracking on shared content | Once imported, content is the user's to modify |
| Notifications about new content | Discovery is manual and intentional |

### Technical implementation

- Import strips non-schema fields from JSON (author, origin, source, createdBy)
- The bundle manifest contains only counts and timestamps — no identity data
- Community content receives the same `IsUserAuthored = true` flag as user content
- No runtime path can distinguish community content from locally authored content
- Export is explicit and user-controlled — never implicit or automated
