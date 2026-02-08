# Dev-Op-Typer (WinUI 3 starter)

A developer-focused typing practice app where **every test is real code** and the full keyboard is used.

## What you get in this starter
- WinUI 3 (Windows App SDK) C# project skeleton
- Single-window layout with:
  - Top bar
  - Code prompt + typing surface
  - Right **collapsible** advanced settings sidebar
- Persistent local profile/settings ("memory") using `ApplicationData.Current.LocalSettings`
- Audio system:
  - Ambient soundscape **Random / Mute** toggle (ambient only)
  - Mechanical keyboard SFX + UI click SFX
  - 50+ ambient WAV placeholders (replace with real files later)

## Requirements
- Visual Studio 2022
- Windows App SDK (WinUI 3)
- Windows 10 1809+ / Windows 11

## Run
1. Open `DevOpTyper.sln` in Visual Studio
2. Restore NuGet packages
3. Run (F5)

## Replace audio
Ambient: `DevOpTyper/Assets/Sounds/Ambient/*.wav`
SFX: `DevOpTyper/Assets/Sounds/Sfx/*.wav`

## Snippet packs
JSON packs live in `DevOpTyper/Assets/Snippets/`.
Each entry has `language`, `difficulty`, `topics`, and `code`.

## Accessibility
The UI is keyboard navigable and includes focus visuals. Expand in `Themes/` resources.
