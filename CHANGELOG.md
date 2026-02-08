# Changelog

All notable changes to Dev-Op-Typer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.2.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.2.0
[0.1.1]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.1
[0.1.0]: https://github.com/mcp-tool-shop-org/dev-op-typer/releases/tag/v0.1.0
