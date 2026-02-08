# Changelog

All notable changes to Dev-Op-Typer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.0] - 2026-02-08

### Theme: Extensible Practice Instrument

v0.6.0 lets advanced users shape how they practice — authoring their own snippets, defining practice configurations, and exporting their work as portable bundles. Creation is personal, not performative. No accounts, no platforms, no gamification of authorship.

### Added
- **User-authored snippets** — Create JSON snippet files in `%LOCALAPPDATA%/DevOpTyper/UserSnippets/`. Loaded on startup, merged into the normal rotation. Same scoring, same XP, same session records as built-in content.
- **Subdirectory organization** — Organize snippets in one level of subdirectories within UserSnippets/. Language derived from filename or declared per-snippet in JSON.
- **Mixed-language collections** — Snippets that declare their own `language` in JSON are grouped by that language, not the filename. Enables collections like "my-favorites.json" spanning multiple languages.
- **Extension boundaries** — `ExtensionBoundary` class declares what users may extend (snippets, configs) and what's frozen (typing engine, XP, persistence, identity). Validates snippet files against limits (50 files, 200 snippets/file, 5000 chars/snippet).
- **Practice configurations** — `PracticeConfig` lets users create named session presets: difficulty bias (easier/harder/match), language override, and typing rule overrides (whitespace, backspace, accuracy floor).
- **Config service** — `PracticeConfigService` discovers configs from `UserConfigs/` directory. Config dropdown in Settings panel with description text.
- **Portable bundles** — Export all user content as a standard ZIP (snippets/ + configs/ + manifest.json). Import bundles via file picker — never overwrites existing files.
- **Data isolation guards** — User content directories validated against app install path. Import sanitizes paths and rejects directory traversal attempts. Built-in assets are read-only.
- **Your Snippets section** — Settings panel shows snippet count, load errors, and Open Folder / Export / Import buttons with access keys.

### Changed
- **"User Snippets" → "Your Snippets"** — Section heading uses personal language, not technical.
- **DifficultyProfile.TargetDifficulty** — Changed from `init` to `set` so practice configs can apply difficulty bias.
- **LanguageTrack.HasUserContent** — Display-only flag for tracks containing user-authored snippets.
- **Snippet.IsUserAuthored** — JsonIgnore flag set during loading; never affects scoring or selection.

### Accessibility
- Snippet status text uses `LiveSetting="Polite"`, errors use `LiveSetting="Assertive"`.
- Status and error TextBlocks are not tab stops — keyboard flows to action buttons.
- Config dropdown has accessible name, tooltip, and description with polite live setting.
- Export/Import/Open Folder buttons have access keys (E/I/O) and descriptive tooltip text.
- Bundle status announced politely after operations.

### Documentation
- **AGENCY.md Extensibility Philosophy** — What may be extended, what's frozen, 6 design constraints, extension safety, verification checklist.
- **Authorship Without Gamification** — No likes, no ratings, no leaderboards. Creation is personal.
- **Portable Bundle Format** — ZIP structure, snippet JSON schema, config JSON schema, manifest format, design guarantees.

### Technical
- **Zero coupling maintained** — All 8 frozen services (TypingEngine, SessionState, PersistenceService, TypistIdentityService, SmartSnippetSelector, AdaptiveDifficultyEngine, TrendAnalyzer, WeaknessTracker) have zero references to v0.6.0 extension types.
- v0.6.0 features live in: UserContentService, PracticeConfigService, PortableBundleService, SnippetService merge logic, and UI wiring.
- Speculative extension points (Warmup, PreferShorterSnippets, FocusTopic) removed before release — if it wasn't wired, it shouldn't exist.
- Frozen service/format/behavior guarantees moved from runtime arrays to design comments.
- No schema version bump — all new features use separate files, not persisted blob changes.

## [0.5.0] - 2026-02-08

### Theme: Companion for Lifelong Practice

v0.5.0 reframes the app from a mastery environment to a companion that ages well with the user. Continuity, resilience, and trust over long time scales.

### Added
- **Long-term identity continuity** — Identity section anchors to "since Month Year" using earliest session date. MinSessions threshold lowered from 15 to 10.
- **Graceful long-gap handling** — Staleness notice reframed from clinical to warm ("your history is here"). Warmup reasons scale by gap duration (30d+, 1d+, 2h+).
- **Orientation cues** — Subtle past-tense context for returning users: last session title/WPM for 30d+ gaps, last session + language distribution for 1d+ gaps. Hidden for same-day.
- **Session framing** — Pre-session context showing typical WPM/accuracy for the current language ("Python · ~45 WPM · ~92%").
- **Per-language session breakdown** — Lifetime stats show top 4 languages with session counts.
- **Plateau detection** — `ComputePlateauLength` counts consecutive sessions within 5% band of mean. Shown as neutral "steady" label in trend rows.
- **Wider-window decline confirmation** — `ComputeDirection` checks a wider window before labeling a trend as Declining, preventing a single bad session from flipping the signal.
- **Adaptive trend thresholds** — Trend detection threshold scales by the user's personal WPM variability (CV). Variable typists get wider bands; steady typists get tighter detection.
- **Practice rhythm observations** — PatternDetector surfaces session burst size ("~3 sessions at a time") and day-of-week preferences ("Practices more on weekdays"). Max observations increased from 2 to 3.
- **Milestone awareness** — Typist identity surfaces factual milestones (50/100/250/500/1000 sessions; 3/6/12/24 months) as facts, not celebrations.
- **Continuity principles doc** — AGENCY.md gains Core Beliefs, Longevity Contract, and "What Knowing the User Means" sections.

### Changed
- **Calmer decline handling** — Declining trend suggestions demoted from Normal to Low priority. Language softened from "declining recently" to "dipped a bit". Fatigue reason changed from "${N} sessions in 30 min" to "You've been at it for a while".
- **No punitive difficulty drops** — StrongNegative momentum no longer reduces target difficulty. Bad stretches widen the available range downward but never force easier snippets.
- **Accuracy capping requires confidence** — Difficulty capping for low accuracy now requires 15+ sessions instead of activating immediately.
- **Screen-reader calm signals** — Trend rows carry accessible names using calm language ("dipped recently" not "declining"). Weakness trajectories use "needs attention" not "worsening". Momentum reads as "steady", "up slightly", "rising", "dipped recently".
- **Informational elements non-focusable** — Identity, pattern, orientation, and pacing blocks set `IsTabStop=false` and `LiveSetting=Off` to avoid screen-reader noise during typing.
- **Suggestion reasons don't narrate logic** — Reasons now express state, not detection method ("You're moving fast" not "+1.2 WPM/session").
- **Internal comments cleaned** — Removed "(Phase X)" and "(vX.Y.Z)" annotations from code comments. Purpose over origin.

### Technical
- `ComputeCV` private method added to TrendAnalyzer for per-user variability computation
- `ComputePlateauLength` private method added to TrendAnalyzer for plateau detection
- `ComputeMilestone` private method added to TypistIdentity for milestone computation
- `DetectSessionBurstPattern` and `DetectDayOfWeekPattern` added to PatternDetector
- Per-language trend sanitization in `SanitizeBlob`: removes NaN/Inf from rolling data, clamps negative TotalSessions
- All v0.5.0 changes maintain zero-coupling guarantee: core services unchanged
- No schema version bump — all changes are computational, not persisted

## [0.4.0] - 2026-02-08

### Theme: Self-Directed Mastery Environment

v0.4.0 puts the developer in control of how and why they practice. The system observes, offers, and never directs.

### Added
- **User-declared practice intent** — Optional intent chips (Focus, Challenge, Maintenance, Exploration) let developers label sessions for their own awareness. Intent is stored but never consumed by any service for scoring, difficulty, or selection.
- **Session retrospection** — Factual completion summary showing WPM/accuracy deltas vs recent average, declared intent, and difficulty context. Observations only — no judgment.
- **Session annotations** — Free-text notes (max 280 chars) attachable to any completed session. Displayed with pencil icon in session history.
- **Intent pattern correlation** — StatsPanel "By Intent" section shows factual averages (WPM, accuracy, session count) grouped by declared intent. Requires 2+ sessions per intent.
- **Practice preferences** — Configurable settings: Show Intent Chips, Default Intent, Practice Note, Focus Area, Show Suggestions.
- **Focus areas** — User-selectable focus (Brackets & Braces, Operators, String Handling, Control Flow, Functions, Data Structures) stored as metadata. Clearable at any time.
- **Pattern detection** — `PatternDetector` produces factual observations (cross-language WPM comparison, time-of-day accuracy patterns, difficulty correlation). Returns strings only — never triggers actions.
- **Suggestion overrides** — Dismiss (×) buttons on every suggestion row. Show Suggestions toggle hides all suggestions. Dismissals are session-scoped only — never persisted.
- **Typist identity** — Longitudinal self-portrait showing primary language, typical WPM range (p10-p90), typical accuracy, practice span, cadence, and "Then vs Now" comparison (earliest vs recent sessions).
- **Consistency metrics** — WPM variability (coefficient of variation) and accuracy steadiness (standard deviation) with neutral descriptors (steady, moderate variation, wide variation). Not gamified.
- **Stale data handling** — Identity data older than 30 days shows neutral "Last active X ago" label and suppresses consistency metrics.
- **Agency philosophy doc** — `AGENCY.md` defines design constraints: system supports/observes/offers, never manages/judges/insists. Includes automation boundary verification and v0.4.0 release audit.

### Changed
- All v0.4.0 sections use `AutomationProperties.HeadingLevel` for consistent screen reader navigation
- All new text blocks have `IsTextSelectionEnabled` and descriptive `AutomationProperties.Name`
- Settings panel sections (Audio, Gameplay, Practice, Typing Rules, Accessibility) all have heading level 2

### Technical
- **Zero coupling guarantee** — All 8 core services (TypingEngine, SmartSnippetSelector, PracticeRecommender, AdaptiveDifficultyEngine, FatigueDetector, SessionPacer, WeaknessTracker, TrendAnalyzer) have zero references to any v0.4.0 feature
- v0.4.0 features live exclusively in UI wiring (MainWindow), panels (Settings, Stats, Typing), and two display-only services (PatternDetector, TypistIdentityService)
- All new fields are nullable with safe defaults — no schema version bump required
- `SanitizeBlob` extended with FocusArea validation, session Note length clamping, and DeclaredIntent enum validation

## [0.3.0] - 2026-02-08

### Added
- **Practice context metadata** — `PracticeContext` captures session intent (Freeform, WeaknessTarget, Repeat, Exploration, Warmup), focus, difficulty snapshot, and rating at start for longitudinal analysis
- **Longitudinal data accumulation** — `LongitudinalData` stores per-language rolling WPM/accuracy (last 50), session timestamps (last 200), and daily weakness snapshots (cap 90)
- **Trend analysis** — `TrendAnalyzer` computes WPM/accuracy direction, velocity (linear regression), and combined momentum per language; requires 5+ sessions for activation
- **Fatigue detection** — `FatigueDetector` observes session cadence (sessions/hour, gap analysis) and classifies as Fresh, Steady, ActivePace, or HighIntensity with non-judgmental labels
- **Practice recommendations** — `PracticeRecommender` suggests weakness targeting, break reminders, neglected language revisits, warmup sessions, and exploration based on longitudinal data
- **Adaptive difficulty engine** — `AdaptiveDifficultyEngine` computes trend-aware target difficulty with range (min/max/target) and confidence, adjusting based on momentum
- **Session pacing** — `SessionPacer` tracks real-time per-launch pacing with lifecycle events and pace labels ("Ready to start", "Warming up", "Good pace", "In the zone", "Intense session")
- **Weakness tracking with trajectory** — `WeaknessTracker` enriches weakness reports with improvement context (Improving, Steady, Worsening, New), resolved weakness detection, and overall trajectory summary
- **Adaptive snippet selection** — `SmartSnippetSelector.SelectAdaptive()` uses difficulty profile range and weakness trajectory multipliers (Worsening 1.5x, New 1.3x, Improving 0.5x)
- **Actionable suggestions** — Suggestion rows in StatsPanel display "Try" buttons for one-click follow-through; `SuggestionAction` enum routes to weakness, warmup, harder, or language-switch snippet loading
- **Practice These button** — Weak spots section shows a "Practice These" button that loads snippets targeting displayed weak characters
- **Session completion banner** — TypingPanel shows results (WPM, accuracy, XP) after session completion with "Perfect!" title for zero-error runs and contextual "Practice weak chars" action
- **Trends section in StatsPanel** — Per-language momentum with direction arrows and recent averages
- **Suggestions section in StatsPanel** — Prioritized practice recommendations with type-specific styling
- **Pacing indicator** — Shows sessions today, time since last session, and pace label in StatsPanel

### Changed
- `LoadNewSnippet` respects Adaptive Difficulty toggle — falls back to basic `SelectNext` when toggle is off
- `StartTest_Click` consumes pending context from action handlers instead of always defaulting to Freeform
- Repeat intent only applied when no other intent is already set
- Completion banner auto-dismissed on Start, Reset, and Skip clicks
- All action paths (weakness practice, easy/harder/language snippets) set appropriate `PracticeContext` with correct intent
- `PersistenceService.SanitizeBlob` extended with null-checks for longitudinal data fields

### Technical
- All v0.3.0 data additions use nullable fields and collection defaults — v0.2.0 persisted state deserializes unchanged with no schema version bump
- `GetTargetDifficultyStatic` exposed as public on `SmartSnippetSelector` for external difficulty queries
- Clean service composition: TrendAnalyzer (standalone), FatigueDetector (standalone), WeaknessTracker (uses TrendAnalyzer), PracticeRecommender (uses both), AdaptiveDifficultyEngine (uses TrendAnalyzer)

## [0.2.0] - 2026-02-08

### Added
- **Per-character visual renderer** — `TypingPresenter` control renders each character with color-coded diff states (correct/error/pending/caret/extra) using batched `Run` elements in a `RichTextBlock`
- **Mistake heatmap** — `MistakeHeatmap` tracks per-character error frequencies with confusion pairs, replacing binary `WeakChars` with frequency-weighted data
- **Weak spots analytics** — StatsPanel shows top 5 weakest characters with error rates, visual error bars, and group weakness aggregation
- **Session history panel** — Recent sessions displayed with WPM, accuracy, errors, XP earned, and perfect run indicators
- **Lifetime stats** — Aggregate statistics (total sessions, averages, personal bests) shown after 3+ sessions
- **Typing rules system** — Configurable whitespace, line endings, trailing spaces, and backspace behavior with `TypingRules` model and `NormalizeText()` preprocessor
- **Adaptive difficulty toggle** — UI control for enabling/disabling smart snippet selection
- **Accuracy floor** — Configurable minimum accuracy threshold (0-100%) below which no XP is earned
- **Anti-grind XP formula** — Speed soft cap above 80 WPM, difficulty multiplier (D1-D5), diminishing returns for repeated snippets, completion bonus
- **CharDiff engine** — `CharDiffAnalyzer` produces typed-vs-target diff arrays with correct/error/pending/extra states and typing rules integration
- **TypingEngine overhaul** — Emits `DiffUpdated`, `ProgressUpdated`, `SessionCompleted`, `TextCorrected` events; supports typing rules and repeat tracking

### Changed
- Per-character heatmap recording is now incremental (O(1) per keystroke, not O(n))
- `PersistenceService.Load()` sanitizes and clamps all deserialized values on load
- `SessionState` guards against NaN/Infinity in WPM, accuracy, and XP calculations
- Empty targets no longer start sessions
- `TypingPresenter` skips re-render when cursor position and diff length are unchanged
- Schema version bumped to v3 with automatic migration from v2 (seeds heatmap from legacy WeakChars)
- `Profile.RecordMiss/RecordHit` update both legacy WeakChars and new Heatmap
- Backup recovery wrapped in independent try/catch for double-corruption resilience

### Fixed
- **Critical**: Heatmap inflation bug — every keystroke re-recorded hits/misses for ALL previously typed characters
- Corrupt state files no longer crash the app (SanitizeBlob clamps impossible values)

## [0.1.1] - 2026-02-07

### Added
- Dynamic audio content system — keyboard themes and soundscapes auto-discovered from filesystem
- Soundscape selector dropdown with 4 categories: Ocean (3), Rain (3), Wind (2), Zen (7)
- 3 new keyboard sound themes: SoftTouch (laptop chiclet), Topre (HHKB dome), AlpsCream (vintage Alps)
- AudioTest project with synthesis generators for ambient tracks and keyboard themes
- Audio.md documentation for audio architecture and contributor workflows
- Gameplay section in settings panel for Hardcore Mode (separated from Accessibility)

### Changed
- Ambient playback stays on same track unless Random button is pressed
- Mute button uses MCI pause/resume instead of stop/close (fixes mute not working)
- Title bar buttons (Random, Mute, Gear) now always register clicks (SetTitleBar drag region fix)
- Settings panel toggles visibility properly on gear button click
- Event handler wiring moved before dropdown population for reliable registration
- Keyboard theme and soundscape selections now persisted in AppSettings

### Removed
- Old Clicky and Thock keyboard themes (replaced with better synthesized alternatives)
- Old flat ambient_01-05.wav files (replaced with categorized soundscape library)

---

## [0.1.0] - 2026-02-07

### Added

#### Core Typing Engine
- Character-by-character diff analysis with `CharDiff` model
- Live WPM and accuracy tracking via `TypingEngine`
- `HardcoreModeEnforcer` for strict error handling (no backspace past errors)
- Session state management (Ready, Running, Completed)

#### User Interface
- Single-window WinUI 3 application
- `TypingPanel` for code input
- `StatsPanel` for live statistics display
- `SettingsPanel` for configuration
- `CodeHighlightPanel` for error highlighting with visual indicators
- `CompletionOverlay` with animated results display
- `SnippetListPanel` with language filtering
- `StatisticsPanel` for progress tracking and personal bests
- Collapsible sidebar for advanced settings

#### Snippets & Learning
- Multi-language snippet support (Python, C#, Java, JavaScript, Bash, SQL)
- `SnippetService` with language tracks and difficulty filtering
- `SmartSnippetSelector` for adaptive learning based on skill rating
- Enhanced `Snippet` model with computed properties (difficulty, topics)

#### Audio System
- `AudioService` with per-channel volume controls (ambient, keyboard, UI)
- Per-channel and master mute toggles
- `KeyboardSoundHandler` with multiple modes (EveryKey, WordsOnly, ErrorsOnly)
- `UiFeedbackService` for UI sound and visual feedback events
- Ambient soundscape with auto-advance between tracks

#### Accessibility
- `AccessibilitySettings` for reduced motion and sensory preferences
- `ThemeManager` for high contrast theme support
- System preference detection (Windows UISettings integration)
- Focus indicators and keyboard navigation support

#### Persistence
- `PersistenceService` with schema versioning and backup
- `AppSettings` with comprehensive preference storage
- `SessionHistory` for tracking completed sessions (up to 500 records)
- `Profile` with XP, levels, and per-language ratings
- `DataExportService` for JSON/CSV export and import with merge options

### Technical
- WinUI 3 / Windows App SDK 1.8
- .NET 10.0 targeting windows10.0.19041.0
- CommunityToolkit.Mvvm 8.3.2 for MVVM support
- Windows.Media.Playback for audio

---

[0.6.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.6.0
[0.5.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.5.0
[0.4.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.4.0
[0.3.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.3.0
[0.2.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.2.0
[0.1.1]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.1
[0.1.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.0
