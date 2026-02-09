# Dev-Op-Typer

**A developer-focused typing practice app for Windows — every test is real code.**

[![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)](VERSION.txt)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)]()
[![Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)]()
[![UI](https://img.shields.io/badge/UI-WinUI%203-blue.svg)]()

> Also available for Linux/macOS: [linux-dev-typer](https://github.com/mcp-tool-shop-org/linux-dev-typer) (Avalonia UI)

## Features

### Real Code Practice
- Type actual code snippets in **Python, JavaScript, C#, Java, SQL, and Bash**
- Character-by-character accuracy tracking with diff highlighting
- Exact symbol matching: `{ } [ ] ( ) < > ; : , . " ' \``
- Newlines and indentation matter

### Adaptive Learning
- Smart snippet selection based on your skill level
- Per-language Elo-like rating system
- Session planner: Target (50%) / Review (30%) / Stretch (20%) mix
- Per-character mistake heatmap with weakness trajectories
- Guided Mode: opt-in weakness-biased selection with micro-drills
- Difficulty scaling (D1–D7) with comfort-zone detection

### Live Statistics
- Real-time WPM, accuracy, and error count
- Session completion with retrospective insights
- Trend tracking: rolling WPM and accuracy per language
- Fatigue detection with break suggestions
- Weak spots panel with character-level analysis

### Teaching & Community
- Scaffolds: progressive context hints with "More context" layers
- Demonstrations: alternative implementations shown as equal peers
- Community signals: display-only tips and difficulty ratings
- Guidance notes from shared content packs
- Skill layers panel for structural understanding

### Content System
- 168+ calibration snippets across 6 languages
- User snippet packs: drop JSON into the packs folder
- Paste Code: paste any code from clipboard as practice content
- Import File/Folder: index source files with auto-detected language
- Export/Import `.ldtpack` bundles for sharing content
- Content-addressed IDs (SHA-256 deduplication)

### Audio
- Ambient soundscapes with multiple themes
- Mechanical keyboard click sounds (5 themes, 8 variations each)
- Per-channel volume controls (ambient, keyboard, UI)
- Mute/unmute from the title bar

### Accessibility
- Full keyboard navigation
- High contrast theme support
- Reduced motion option
- AutomationProperties on all interactive elements

### Persistence
- Profile with XP, levels, and per-language ratings
- Settings and language selection saved across sessions
- Session history (up to 500 records) with monthly compression
- Practice configs: named parameter sets for engine tuning

## Install

### Microsoft Store (recommended)
Coming soon — pending Store certification.

### Build from Source

**Requirements:**
- Windows 10 version 1809+ or Windows 11
- .NET 10.0 SDK
- Visual Studio 2022 (with Windows App SDK workload) — or CLI

```bash
git clone https://github.com/mcp-tool-shop-org/dev-op-typer.git
cd dev-op-typer
dotnet build DevOpTyper/DevOpTyper.csproj -c Release -p:Platform=x64
```

Run the built executable:
```
DevOpTyper\bin\x64\Release\net10.0-windows10.0.19041.0\DevOpTyper.exe
```

## Project Structure

```
DevOpTyper/
├── Assets/
│   ├── Icons/         # App icons and Store tile assets
│   ├── Snippets/      # JSON snippet packs by language
│   └── Sounds/        # Ambient and SFX audio files
├── Controls/          # Custom controls (CodeRenderer, TypingPresenter)
├── Models/            # Data models (Profile, Snippet, AppSettings, etc.)
├── Panels/            # UI panels (Typing, Stats, Settings, Explanation, etc.)
├── Services/          # Core services (Audio, Typing, Persistence, Content)
├── Themes/            # Color and high-contrast themes
├── MainWindow.xaml    # Main application window
└── Package.appxmanifest  # MSIX packaging manifest
external/
└── meta-content-system/  # Shared content library (submodule)
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Tab / Shift+Tab | Navigate controls |
| Enter | Start new test |
| Escape | Reset current test |

## Adding Your Own Code

There are three ways to practice your own code:

### Option 1: Paste Code (easiest)

1. Open the **Settings** panel (click ⚙ in the title bar)
2. Scroll to **Paste Code**
3. Paste any code snippet into the text box
4. Click **Add** — the language is auto-detected
5. Your code appears in the snippet rotation immediately

### Option 2: Import a File or Folder

1. Open **Settings** → scroll to **Import**
2. Click **Import File** to add a single source file, or **Import Folder** to scan an entire project
3. The app auto-detects language from file extensions (`.py`, `.js`, `.cs`, `.java`, `.sql`, `.sh`)
4. Imported code is deduplicated by content hash — the same code is never added twice

### Option 3: Create a Snippet Pack (JSON)

For curated sets of practice snippets:

1. Open your user snippets folder:
   ```
   %LocalAppData%\DevOpTyper\UserSnippets\
   ```
   (or click **Open Snippets Folder** in Settings)

2. Create a JSON file named after the language (e.g. `python.json`):
   ```json
   {
     "language": "python",
     "snippets": [
       {
         "id": "my_list_comp",
         "title": "List comprehension",
         "difficulty": 3,
         "topics": ["lists", "comprehension"],
         "code": "squares = [x**2 for x in range(10)]\n"
       },
       {
         "id": "my_dict_comp",
         "title": "Dictionary comprehension",
         "difficulty": 4,
         "topics": ["dicts", "comprehension"],
         "code": "counts = {word: len(word) for word in words}\n"
       }
     ]
   }
   ```

3. Restart the app — your snippets appear alongside the built-in ones

**Tips:**
- `id` must be unique across all packs
- `difficulty` ranges from 1 (easy) to 7 (hard)
- `code` should end with `\n`
- You can organize packs in subdirectories one level deep

### Sharing Content

Export your custom snippets as a portable `.ldtpack` bundle:

1. Open **Settings** → click **Export Bundle**
2. Share the `.ldtpack` file with others
3. They import it via **Settings** → **Import Bundle**

Only user-authored content travels — never practice history or settings.

## Privacy

Dev-Op-Typer is fully offline. No data is collected, transmitted, or shared. See [PRIVACY.md](PRIVACY.md).

## License

[MIT](LICENSE)
