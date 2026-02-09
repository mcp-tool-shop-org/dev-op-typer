# DevOpTyper Audio Integration Roadmap — v0.1.1

This document provides everything needed to integrate the DevOpTyper audio system into any platform (Linux, macOS, web). The goal is consistent sound identity across all versions while allowing platform-specific playback engines.

---

## Shared Audio Assets

All WAV files in this package are platform-independent 16-bit PCM. They are the **single source of truth** for the DevOpTyper sound identity. Both the Windows (WinUI 3) and Linux versions should ship the same files.

### File Inventory

**Keyboard SFX** — 5 themes, 8 variations each = 40 files + 1 UI click

| Theme | Files | Duration | Character |
|-------|-------|----------|-----------|
| `Sfx/AlpsCream/key_01-08.wav` | 8 | 110 ms | Vintage Alps damped linear, warm metallic leaf spring |
| `Sfx/Mechanical/key_01-08.wav` | 8 | varies | CC0 Cherry MX real recordings |
| `Sfx/Membrane/key_01-08.wav` | 8 | 90 ms | Rubber dome office keyboard, muted thud |
| `Sfx/SoftTouch/key_01-08.wav` | 8 | 60 ms | Quiet laptop chiclet, gentle taps |
| `Sfx/Topre/key_01-08.wav` | 8 | 120 ms | HHKB-style dome collapse + spring pop |
| `Sfx/ui_click.wav` | 1 | short | Button/UI feedback click |

**Ambient Soundscapes** — 4 categories, 15 tracks total

| Soundscape | Files | Character |
|------------|-------|-----------|
| `Ambient/Ocean/` | 3 | Gentle waves, pebble shore, coastal breeze |
| `Ambient/Rain/` | 3 | Steady rain, heavy downpour, thunderstorm |
| `Ambient/Wind/` | 2 | Alpine meadow, pine forest |
| `Ambient/Zen/` | 7 | Singing bowl, om drone, ethereal pad, crystal harmonics, warm fifths, deep earth drone, celestial wash |

---

## Audio Spec Contract

Both platforms MUST support these specs for consistent behavior:

### Keyboard SFX

| Property | Required | Notes |
|----------|----------|-------|
| Format | 44100 Hz, 16-bit PCM, Mono WAV | All shipped files use this |
| Polyphonic | Yes | Multiple key sounds must overlap (typing fast) |
| Latency | < 15 ms | Perceptible delay ruins the typing feel |
| Random selection | Yes | Pick random `key_NN.wav` per keypress |
| Pre-loading | Recommended | Decode all WAVs into memory at startup |
| Volume control | Per-channel (0.0-1.0) | Keyboard, Ambient, UI are independent |

### Ambient Soundscapes

| Property | Required | Notes |
|----------|----------|-------|
| Format | 44100 Hz, 16-bit PCM, WAV | Mono or Stereo |
| Looping | Yes | Seamless repeat of single track |
| Pause/Resume | Yes | Mute must pause, not stop+restart |
| Track persistence | Yes | Same track plays until user presses Random |
| Volume control | Independent from SFX | Adjustable 0-100 slider |

---

## Directory Layout Convention

Both platforms should use this exact directory structure so contributors can drop files in either repo:

```
assets/sounds/
  sfx/
    ui_click.wav
    {ThemeName}/
      key_01.wav
      key_02.wav
      ...
      key_08.wav
  ambient/
    {SoundscapeName}/
      {descriptive_name}.wav
      ...
```

- **Theme/soundscape discovery is filesystem-based** — scan subdirectories at startup
- Folder name = display name in UI dropdown
- No hardcoded theme or soundscape lists in code
- Case-sensitive on Linux, case-insensitive on Windows — use consistent casing

---

## Platform-Specific Playback Engines

### Windows (current — v0.1.1)

| Channel | Engine |
|---------|--------|
| Keyboard SFX | NAudio `WasapiOut` shared mode + `MixingSampleProvider` |
| Ambient | Win32 `mciSendString` with `mpegvideo` type |
| Pre-loading | `AudioFileReader` → `float[]` buffer per file |

### Linux (recommended)

| Channel | Engine Options |
|---------|---------------|
| Keyboard SFX | **PipeWire/PulseAudio** via SDL2, OpenAL Soft, or miniaudio |
| Ambient | Same engine, separate channel/source |
| Pre-loading | Decode WAV to `float[]` or `int16[]` at startup |

Recommended libraries for Linux:
- **miniaudio** (C, single-header) — cross-platform, zero dependencies, supports PipeWire/PulseAudio/ALSA
- **SDL2_mixer** — well-tested, multiple channels, available on all distros
- **OpenAL Soft** — 3D audio API but works great for 2D, supports mixing

Key requirements:
- Must support playing multiple SFX simultaneously (typing produces rapid overlapping sounds)
- Must support looping a single ambient track
- Must support independent volume per channel
- Must support pause/resume without restarting the track

---

## Settings Persistence

Both platforms should persist these audio settings between sessions:

```json
{
  "ambientVolume": 0.5,
  "keyboardVolume": 0.7,
  "uiClickVolume": 0.6,
  "keyboardSoundTheme": "Mechanical",
  "selectedSoundscape": "Zen"
}
```

Default values shown above. Store in platform-appropriate location:
- **Windows**: `%LOCALAPPDATA%\DevOpTyper\settings.json`
- **Linux**: `~/.config/devop-typer/settings.json` or XDG equivalent

---

## UI Controls

Both platforms should expose these controls with consistent naming:

### Title Bar / Toolbar

| Control | Behavior |
|---------|----------|
| **Random** button | Play a random track from the current soundscape |
| **Mute** button | Toggle pause/resume on ambient (NOT stop/restart) |

### Settings Panel

| Control | Type | Notes |
|---------|------|-------|
| **Ambient Volume** | Slider 0-100 | Controls soundscape volume |
| **Keyboard Sound Volume** | Slider 0-100 | Controls key SFX volume |
| **Keyboard Sound** | Dropdown | Lists discovered theme folders |
| **UI Sound Volume** | Slider 0-100 | Controls ui_click.wav volume |
| **Soundscape** | Dropdown | Lists discovered ambient folders |

---

## Contributor Guide — Adding Audio Content

### Adding a new keyboard theme

1. Create a folder: `assets/sounds/sfx/MyTheme/`
2. Add 4-16 WAV files named `key_01.wav`, `key_02.wav`, etc.
3. Specs: 44100 Hz, 16-bit PCM, mono, 60-150 ms, peak at -1 to -3 dBFS
4. Rebuild/restart — theme appears in dropdown automatically

### Adding a new soundscape

1. Create a folder: `assets/sounds/ambient/MyScene/`
2. Add 1+ WAV files with descriptive names (e.g., `gentle_rain.wav`)
3. Specs: 44100 Hz, 16-bit PCM, mono or stereo, 30s-5min, seamless loop preferred
4. Rebuild/restart — soundscape appears in dropdown automatically

### Converting existing audio

```bash
# Convert any audio to keyboard SFX spec
ffmpeg -i input.wav -ar 44100 -ac 1 -sample_fmt s16 key_01.wav

# Convert any audio to ambient spec (stereo ok)
ffmpeg -i input.wav -ar 44100 -sample_fmt s16 ambient_track.wav

# Trim to specific duration (e.g., 100ms for keyboard)
ffmpeg -i input.wav -ar 44100 -ac 1 -sample_fmt s16 -t 0.1 key_01.wav
```

### Free audio sources

| Source | License | Best for |
|--------|---------|----------|
| [Freesound.org](https://freesound.org) | CC0 / CC-BY | Field recordings, loops |
| [Kenney Assets](https://kenney.nl/assets?q=audio) | CC0 | UI sounds, digital SFX |
| [Orange Free Sounds](https://orangefreesounds.com/) | CC-BY / Public Domain | Nature recordings |
| [BBC Sound Effects](https://sound-effects.bbcrewind.co.uk/) | RemArc | High-fidelity nature |

### Keyboard sound packs to adapt

| Project | Switch profiles available |
|---------|--------------------------|
| **Mechvibes** | Community packs, folder-per-switch |
| **keyboard-sounds** | 15 bundled profiles |
| **Daktilo** | Vintage typewriter + IBM buckling spring |

---

## Synthesis Generators

The `AudioTest/` project (C#/.NET) contains generators that produce the synthesized themes:

| File | Generates |
|------|-----------|
| `GenerateThemes.cs` | Membrane theme |
| `GenerateNewThemes.cs` | SoftTouch, Topre, AlpsCream + candidates |
| `GenerateAmbient.cs` | All 15 ambient soundscape tracks |
| `GenerateZenDrones.cs` | Additional Zen drone/pad variants |

These are reference implementations. The Linux version can either:
1. **Use the same WAV files** (recommended — this zip has them all)
2. Port the synthesis to Python/Rust/C if you want to regenerate or customize

---

## Version Parity Checklist

Use this to verify both platforms match:

- [ ] 5 keyboard themes present (AlpsCream, Mechanical, Membrane, SoftTouch, Topre)
- [ ] 4 soundscape categories present (Ocean, Rain, Wind, Zen)
- [ ] 15 ambient tracks total (3+3+2+7)
- [ ] 41 SFX files total (40 keys + 1 ui_click)
- [ ] Keyboard theme dropdown auto-populated from filesystem
- [ ] Soundscape dropdown auto-populated from filesystem
- [ ] Random button shuffles within current soundscape
- [ ] Mute pauses/resumes (does not restart track)
- [ ] Independent volume sliders for Ambient, Keyboard, UI
- [ ] Settings persisted between sessions
- [ ] Polyphonic keyboard SFX (rapid typing produces overlapping sounds)
- [ ] Ambient loops seamlessly
