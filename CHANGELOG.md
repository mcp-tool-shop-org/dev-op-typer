# Changelog

All notable changes to Dev-Op-Typer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.2] - 2026-02-08

### Theme: Parity + Migration Prep

v0.8.2 makes CodeItem the canonical content shape and replaces hardcoded difficulty defaults with deterministic, metrics-based derivation. Difficulty expands from 5 tiers to 7, with three scoring bands (line count, symbol density, nesting depth) mapped linearly. Authored difficulty from snippet metadata always takes precedence over derived values.

### Changed
- **Difficulty scale expanded to 1-7** — Replaces the 1-5 range with finer-grained tiers: Trivial, Easy, Moderate, Intermediate, Challenging, Advanced, Expert. All rating-to-difficulty mappings updated with 100-point Elo bands.
- **Deterministic difficulty derivation** — `DifficultyEstimator` now uses three explicit bands (lines 0-3pts, symbol density 0-3pts, indent depth 0-3pts) mapped to difficulty 1-7. No ML, no adaptation, fully reproducible.
- **XP formula extended** — Difficulty multiplier covers D1 (0.5x) through D7 (2.0x) with smooth progression. Mid-range D4 is the new 1.0x baseline.
- **Validation parity checks** — `ContentIntegrationValidator` gains `ValidateDifficultyDerivation` (score-to-tier mapping, determinism, all tiers reachable) and `ValidateNoDifficultyDefault` (multiple distinct tiers in built-in content).

### Technical
- `DifficultyEstimator.Estimate()` returns 1-7 via `Clamp(1 + score * 6 / 9, 1, 7)`.
- `SmartSnippetSelector.GetTargetDifficulty()` and `AdaptiveDifficultyEngine.RatingToDifficulty()` expanded to 7 tiers with consistent rating bands.
- `ExtensionBoundary.ValidateSnippetFile()` accepts difficulty 1-7.
- `PersistenceService.SanitizeBlob()` clamps session difficulty to 1-7.
- All `Math.Min(5, ...)` and `Math.Clamp(..., 1, 5)` updated to 7.
- `Snippet.DifficultyLabel` returns 7 named tiers.
- CI fixed: `submodules: recursive` added to checkout step.

## [0.8.1] - 2026-02-08

### Theme: Content System Integration

v0.8.1 unifies all content — built-in, user-pasted, and corpus-imported — through a single CodeItem/IContentLibrary pipeline powered by meta-content-system v1.0.0. Every piece of code gets the same storage, selection, and metrics — whether it shipped with the app, was pasted from a clipboard, or was imported from a folder.

### Added
- **Unified content pipeline** — All content flows through `CodeItem → ContentBridge → Snippet`. Built-in snippets, user-pasted code, and folder-imported corpus items share the same query, selection, and metrics infrastructure.
- **meta-content-system v1.0.0** — Git submodule providing `CodeItem`, `IContentLibrary`, `LibraryIndexBuilder`, `ContentId` (SHA-256 hash), `Normalizer`, `MetricCalculator`, `DefaultExtractor`, `LanguageDetector`.
- **Paste Code** — Paste code from clipboard directly into the library. Language auto-detected, normalized, metrics computed, deduplicated. Available for practice immediately. AccessKey="V".
- **Import Folder** — Pick a local folder of code files — they're indexed using `LibraryIndexBuilder` pipeline (extract, normalize, metrics, dedup) and available for practice. Runs on background thread with progress text. AccessKey="F".
- **ContentLibraryService** — Replaces `SnippetService` as the single unified content service. Owns `InMemoryContentLibrary`, `BuiltinOverlayStore`, `JsonLibraryIndexStore`. Full query API: `GetSnippets`, `GetLanguageTracks`, `GetSnippetsByTopic`, `GetSnippetById`, `GetRandomSnippet`.
- **SnippetOverlay** — Educational metadata (Difficulty, Topics, Explain, Scaffolds, Demonstrations, Layers, Perspectives) keyed by content-hash ID. Built-in overlays extracted from `Assets/Snippets/*.json`. User/corpus items get heuristic difficulty from `DifficultyEstimator`.
- **ContentBridge** — Static mapper `CodeItem + SnippetOverlay? → Snippet`. Snippet becomes a ViewModel populated from CodeItem + educational overlay.
- **DifficultyEstimator** — Heuristic difficulty 1-5 from `CodeMetrics` (lines, symbol density, indent depth) for content without overlays.
- **FolderContentSource** — `IContentSource` for local folder import. Filters by code extensions, skips binary/vendor directories, respects 2MB file size limit.
- **Library stats in Settings** — "My Library" section showing item counts by source (built-in, pasted, imported, total).
- **Integration validator** — Debug-only runtime validation: built-in content parity, legacy ID preservation, query API consistency, paste flow, persistence round-trip, resilience (missing/corrupt index recovery), query performance.

### Technical
- **Legacy ID preservation** — Built-in `Snippet.Id` stays as human-authored string (e.g. `"py-guard-clause"`) via `SnippetOverlay.LegacyId`. Session history, scaffold fade, community signals, guidance lookups continue working unchanged.
- **library.index.json** — Persists only user/corpus `CodeItem`s at `%LOCALAPPDATA%/DevOpTyper/`. Built-in items always rebuilt from assets at startup. Missing or corrupt index → graceful recovery with built-ins.
- **SnippetService deleted** — Replaced by `ContentLibraryService`. `UserContentService` and `CommunityContentService` retained for directory management and bundle export/import.

### Boundaries
- **MaxLibraryUserItems = 500** — Maximum pasted items in the library
- **MaxLibraryCorpusItems = 2000** — Maximum folder-imported items
- **MaxPasteLength = 10000** — Maximum characters for pasted code
- **MaxImportFileSize = 2MB** — Files larger than 2MB skipped during folder import

## [0.8.0] - 2026-02-08

### Theme: Quiet Teaching & Mentorship

v0.8.0 lets developers learn from quiet demonstrations without turning practice into a course. Pedagogy without hierarchy — optional scaffolds, alternative approaches, contextual guidance, and layered depth. No curricula, no teachers, no progress gates. Just material that speaks to the learner at whatever level they choose.

### Added
- **Scaffold hints** — Optional `Scaffolds` field on snippets: short cues that help users notice patterns ("Watch the bracket alignment", "Semicolons at line ends"). Displayed below community hints during practice.
- **Scaffold fading** — `ScaffoldFadeService` computes opacity from per-snippet completion history. 0 completions → full opacity; 1 good → dimmed; 2+ good → faint; 3+ → hidden. Stateless — no persisted fade state, recomputed each load.
- **Demonstrations** — `Demonstration` model (Label + Code + Description) for alternative approaches to the same problem. Multiple approaches coexist without ranking. No author attribution. Labels describe the approach, never the author.
- **DemonstrationPanel** — Collapsible panel showing alternative approaches between sessions. Each demonstration in its own bordered card. Hidden during active typing. Code blocks are text-selectable.
- **Guidance notes** — `GuidanceNote` model and `GuidanceService` load contextual observations from `guidance.json` in CommunityContent/. Collective language, display-only. Dismissible per-session with no dismissal tracking.
- **Guidance in UI** — Subtle guidance area below scaffold hints using collective language ("Many find the nesting tricky here"). Dismiss button hides for current snippet only. Next snippet re-shows.
- **Skill layers** — `SkillLayer` model (Label + Content[]) for depth tiers on snippets. Labels describe depth ("Essentials", "Deeper", "Advanced"), not the user. All layers accessible to all users at all times.
- **LayersPanel** — Collapsible panel with individually-expandable layers. All layer labels visible, content starts collapsed. First layer uses primary foreground; deeper layers use secondary. No "recommended" or "start here" labeling.
- **Four teaching toggles** — `ShowScaffolds`, `ShowDemonstrations`, `ShowGuidance`, `ShowSkillLayers` in AppSettings. Each independently toggleable, defaults to true, zero impact on scoring when disabled. Real-time UI response.
- **Teaching section in Settings** — Dedicated section with all four toggles, descriptive headers, and immediate visual feedback.
- **Guidance in portable bundles** — `guidance.json` included in bundle export/import alongside `signals.json`.

### Boundaries
- **MaxScaffoldHints = 5** — Scaffolds are short cues; too many defeats their purpose
- **MaxScaffoldHintLength = 200** — Hints must be brief enough to scan at a glance
- **MaxDemonstrationsPerSnippet = 3** — Keeps the demonstration panel focused and scannable
- **MaxDemonstrationCodeLength = 3000** — Reasonable limit for alternative code approaches
- **MaxDemonstrationDescriptionLength = 200** — Descriptions stay concise
- **MaxGuidanceNotesPerSnippet = 5** — Guidance notes are short collective observations
- **MaxGuidanceNoteLength = 200** — Notes must be brief observations, not instruction
- **MaxLayersPerSnippet = 4** — Layers offer depth, not breadth
- **MaxContentPerLayer = 8** — Keeps individual layers scannable
- **MaxLayerContentLength = 300** — Short content stays descriptive

### Accessibility
- ScaffoldHints: `IsTabStop="False"`, `LiveSetting="Off"`, non-interrupting during typing.
- DemonstrationPanel: `LiveSetting="Off"` on content area. ToggleButton `TabIndex="201"` with tooltip. Code blocks `IsTextSelectionEnabled="True"`, `IsTabStop="False"`. Labels have HeadingLevel.Level3.
- GuidanceArea: `IsTabStop="False"`, `LiveSetting="Off"`. Dismiss button has `AutomationProperties.Name="Dismiss guidance"`. Hidden during active typing.
- LayersPanel: ToggleButton `TabIndex="202"`. Each layer expandable via keyboard. Labels have HeadingLevel.Level3. `LiveSetting="Off"` on content.
- Teaching section in Settings: Heading Level 2, descriptive `AutomationProperties.Name` on all toggles.

### Documentation
- **Teaching Philosophy** — Pedagogy without hierarchy, scaffolds as optional supports, demonstrations over explanations, mentorship without mentors, learning without levels
- **Demonstrations as Alternatives, Not Corrections** — Multiple valid approaches, no ranking, no "the answer", display-only between sessions
- **Mentorship Without Mentors** — Guidance from collective experience, no attribution, always dismissible, collective language rules, optional enhancement never dependency
- **Inclusive Learning Without Levels** — Labels describe depth not users, no content gating, no beginner/expert modes, no curricula, no completion certificates
- **Frozen Service Audit Matrix** — 8 frozen services × 7 v0.8.0 types = zero references confirmed

### Technical
- **Zero coupling maintained** — All 8 frozen services (TypingEngine, SessionState, PersistenceService, SmartSnippetSelector, AdaptiveDifficultyEngine, TrendAnalyzer, WeaknessTracker, TypistIdentityService) have zero references to any v0.8.0 type (verified by grep at release).
- v0.8.0 features live in: ScaffoldFadeService, DemonstrationPanel, GuidanceService, LayersPanel, Snippet model extensions, and UI wiring.
- New persisted fields: `AppSettings.ShowScaffolds/ShowDemonstrations/ShowGuidance/ShowSkillLayers` (all default safely to true). Guidance data lives on filesystem, not in PersistedBlob.
- No network calls — all teaching content is local: inline on snippets (scaffolds, demonstrations, layers) or in community files (guidance.json).
- No identity — no teacher/student roles, no author attribution, no progress gating.
- Backward compatible — removing teaching data from snippets and deleting guidance.json returns app to exact v0.7.0 behavior. All new Snippet fields are arrays defaulting to empty.

## [0.7.0] - 2026-02-08

### Theme: Community Without Platforms

v0.7.0 lets developers quietly learn from each other without turning practice into a social performance. Community means shared craft — no accounts, no feeds, no rankings, no attribution pressure.

### Added
- **Community content directory** — `%LOCALAPPDATA%/DevOpTyper/CommunityContent/` for content from others, parallel to UserSnippets/ and UserConfigs/. Organizational separation on disk, indistinguishable at runtime.
- **CommunityContentService** — Discovers and loads snippets from CommunityContent/ using the same validation as user content. Sets `IsUserAuthored = true` — no "foreign" distinction at runtime.
- **Import target selection** — Bundle import shows "My Content" / "Community Content" dialog. Routes to the appropriate directory based on user choice.
- **Community config scanning** — PracticeConfigService scans CommunityContent/configs/ alongside UserConfigs/. Community configs appear in dropdown with no visual distinction.
- **Snippet perspectives** — New `ExplanationSet` model (Label + Notes[]) and `Perspectives` property on Snippet. Multiple viewpoints coexist without hierarchy. Labels describe focus, never authors.
- **ExplanationPanel** — Collapsible panel showing perspectives between sessions. Hidden during active typing. Legacy `explain` fields merged as "Notes" perspective.
- **Aggregate signals** — `AggregateSignal` model for anonymized collective hints (typical WPM, common difficulties, free-text hint). Loaded from signals.json by display-only `CommunitySignalService`.
- **Community hints in UI** — Subtle hint below snippet title using collective language ("typically ~45 WPM", "often tricky: nested braces"). Never comparative.
- **Signal opt-out** — `ShowCommunitySignals` setting (default true). Disabling hides all hints immediately with zero penalty or behavior change.
- **Content age tracking** — `GetContentAge()` and `GetContentSummary()` on CommunityContentService. Settings shows enhanced summary with age info for old content.
- **Community section in Settings** — Content status, Open Folder, Import Bundle, and signal toggle in one cohesive area.
- **Import sanitization** — `ExtractAndSanitize()` strips non-schema fields (author, origin, source, createdBy) from snippet JSON during import, including nested perspectives.
- **Export includes community** — Bundle export optionally includes community content alongside user content. Signals.json exported if present.

### Boundaries
- **MaxPerspectivesPerSnippet = 5** — Prevents overwhelming the user with viewpoints
- **MaxNotesPerPerspective = 10** — Keeps each perspective focused
- **MaxExplanationNoteLength = 300** — Short notes stay descriptive; long notes truncated
- **MaxCommunitySnippetFiles = 100** — Higher than user limit for accumulated imports
- **MaxCommunityConfigs = 40** — Reasonable ceiling for shared configs

### Accessibility
- ExplanationPanel: `LiveSetting="Off"` prevents screen reader interruptions during typing. Toggle reachable by keyboard (TabIndex=200). Perspective labels have HeadingLevel.Level3.
- Community hint: `IsTabStop="False"`, `LiveSetting="Off"`, text-selectable but non-interrupting.
- Community section: Heading Level 2, status with Polite live setting, buttons with access keys and tooltips.

### Documentation
- **Community Philosophy** — Core principle, what community means/doesn't mean, verification checklist
- **Sharing Without Social Signaling** — No origin tracking, no badges, no download counts
- **Explanations as Context, Not Instructions** — Descriptive not prescriptive, multiple perspectives prevent single-voice authority
- **Aggregate Signal Ethics** — Signals reflect patterns not people, collective not comparative language, opt-out guarantee
- **Permanent Constraints** — No accounts, no feeds, no followers, no leaderboards, no network calls, no telemetry

### Technical
- **Zero coupling maintained** — All 8 frozen services have zero references to v0.7.0 types (verified by grep at release).
- v0.7.0 features live in: CommunityContentService, CommunitySignalService, ExplanationPanel, PortableBundleService extensions, and UI wiring.
- New persisted field: `AppSettings.ShowCommunitySignals` (defaults safely to true). Community data lives on filesystem, not in PersistedBlob.
- No network calls — all sharing is local file operations (ZIP import/export, filesystem directories).
- Backward compatible — removing CommunityContent/ returns app to exact v0.6.0 behavior.

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

[0.8.2]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.8.2
[0.8.1]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.8.1
[0.8.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.8.0
[0.7.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.7.0
[0.6.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.6.0
[0.5.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.5.0
[0.4.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.4.0
[0.3.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.3.0
[0.2.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.2.0
[0.1.1]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.1
[0.1.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.0
