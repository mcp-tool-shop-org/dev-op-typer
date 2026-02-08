# Dev-Op-Typer

**A developer-focused typing practice app where every test is real code.**

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](VERSION.txt)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)]()
[![Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)]()

## Features

### 🎯 Real Code Practice
- Type actual code snippets in Python, C#, Java, JavaScript, Bash, and SQL
- Character-by-character accuracy tracking
- Exact symbol matching: `{ } [ ] ( ) < > ; : , . " ' \``
- Newlines and indentation matter

### 📊 Live Statistics
- Real-time WPM and accuracy display
- Error highlighting with visual indicators
- Session completion overlay with results
- Personal bests tracking

### 🎮 Adaptive Learning
- Smart snippet selection based on your skill level
- Per-language rating system (Elo-like)
- Difficulty scaling as you improve
- Hardcore mode: no backspace past errors

### 🎵 Immersive Audio
- Ambient soundscapes (mutable independently)
- Mechanical keyboard click sounds
- UI feedback sounds
- Per-channel volume controls

### ♿ Accessibility
- Full keyboard navigation
- High contrast theme support
- Reduced motion option
- Screen reader optimizations
- Focus indicators on all controls

### 💾 Persistence
- Profile with XP and levels
- Settings saved across sessions
- Session history (up to 500 records)
- Export/import data as JSON or CSV

## Requirements

- Windows 10 version 1809+ or Windows 11
- Visual Studio 2022 (with Windows App SDK workload)
- .NET 10.0 SDK

## Quick Start

1. Open `DevOpTyper.sln` in Visual Studio 2022
2. Restore NuGet packages (automatic)
3. Select **Debug | x64** configuration
4. Press **F5** to run

## Project Structure

```
DevOpTyper/
├── Assets/
│   ├── Snippets/     # JSON snippet packs by language
│   └── Sounds/       # Ambient and SFX audio files
├── Models/           # Data models (Profile, Snippet, etc.)
├── Panels/           # UI panels (Typing, Stats, Settings)
├── Services/         # Core services (Audio, Typing, Persistence)
├── Themes/           # Color and high-contrast themes
└── MainWindow.xaml   # Main application window
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Tab / Shift+Tab | Navigate controls |
| Enter | Start new test |
| Escape | Reset current test |
| Ctrl+, | Toggle settings sidebar |

## Audio Files

Replace placeholder audio files in:
- **Ambient**: `Assets/Sounds/Ambient/*.wav`
- **SFX**: `Assets/Sounds/Sfx/*.wav` (key_*.wav, ui_click.wav)

## Adding Snippets

Snippet packs are JSON files in `Assets/Snippets/`:

```json
{
  "language": "python",
  "snippets": [
    {
      "id": "py_hello",
      "title": "Hello World",
      "difficulty": 1,
      "topics": ["basics", "print"],
      "code": "print(\"Hello, World!\")\n"
    }
  ]
}
```

## License

See [LICENSE](LICENSE) for details.

---

**Version 0.1.0** - See [CHANGELOG.md](CHANGELOG.md) for release notes.
