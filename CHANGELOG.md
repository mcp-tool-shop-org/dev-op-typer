# Changelog

All notable changes to Dev-Op-Typer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.1.0]: https://github.com/user/dev-op-typer/releases/tag/v0.1.0
