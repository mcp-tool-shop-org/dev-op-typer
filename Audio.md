# DevOpTyper Audio System — v0.1.1

## Architecture

DevOpTyper uses a dual-engine audio system:

| Channel | Engine | Format | Playback |
|---------|--------|--------|----------|
| **Keyboard SFX** | NAudio WASAPI shared mode | 44100 Hz, 16-bit PCM mono `.wav` | Polyphonic via `MixingSampleProvider`, pre-decoded into memory buffers |
| **Ambient / Soundscapes** | Win32 `mciSendString` (`mpegvideo` type) | Any `.wav` | Single-track looping, random selection per session |

All audio content is discovered dynamically at startup from the filesystem. No code changes are needed to add new themes or soundscapes — drop a folder, rebuild, done.

---

## Directory Structure (v0.1.1)

```
Assets/Sounds/
  Sfx/
    ui_click.wav                # UI feedback (shared across all themes)
    AlpsCream/                  # Vintage Alps SKCM Cream Damped (synthesized)
      key_01.wav ... key_08.wav
    Mechanical/                 # CC0 Cherry MX recordings
      key_01.wav ... key_08.wav
    Membrane/                   # Synthesized rubber dome
      key_01.wav ... key_08.wav
    SoftTouch/                  # Quiet laptop chiclet (synthesized)
      key_01.wav ... key_08.wav
    Topre/                      # HHKB-style dome collapse + spring pop (synthesized)
      key_01.wav ... key_08.wav
  Ambient/
    Ocean/
      ocean_coastal_breeze.wav
      ocean_gentle_waves.wav
      ocean_pebble_shore.wav
    Rain/
      rain_heavy_downpour.wav
      rain_steady.wav
      rain_thunderstorm.wav
    Wind/
      wind_alpine_meadow.wav
      wind_pine_forest.wav
    Zen/
      zen_celestial_wash.wav
      zen_crystal_harmonics.wav
      zen_deep_earth_drone.wav
      zen_ethereal_pad.wav
      zen_om_drone.wav
      zen_singing_bowl.wav
      zen_warm_fifths.wav
```

The folder name becomes the display name in the Settings panel dropdown.

---

## Adding a Keyboard Theme

1. Create a directory under `Assets/Sounds/Sfx/` (e.g. `HolyPanda/`)
2. Add 4-16 files named `key_01.wav`, `key_02.wav`, etc.
3. Build. The theme appears in the **Keyboard Sound** dropdown automatically.

### Audio specs

| Property | Value |
|----------|-------|
| Sample rate | 44100 Hz |
| Bit depth | 16-bit PCM |
| Channels | Mono (auto-upmixed to stereo at playback) |
| Duration | 60-150 ms recommended |
| Peak level | -1 to -3 dBFS |
| Naming | `key_01.wav` through `key_NN.wav` |

The engine picks a random file from the set on every keypress. More files = more natural variation.

### Current themes

| Theme | Type | Description | Duration |
|-------|------|-------------|----------|
| **AlpsCream** | Synthesized | Vintage Alps damped linear, warm metallic leaf spring | 110 ms |
| **Mechanical** | CC0 recording | Real Cherry MX switch recordings | varies |
| **Membrane** | Synthesized | Rubber dome, muted thud with housing resonance | 90 ms |
| **SoftTouch** | Synthesized | Quiet laptop chiclet, gentle taps | 60 ms |
| **Topre** | Synthesized | HHKB-style dome collapse + spring pop | 120 ms |

### Synthesized theme generators

The `AudioTest/` project contains synthesis generators:

- `GenerateThemes.cs` — Membrane (original)
- `GenerateNewThemes.cs` — SoftTouch, Topre, AlpsCream, and other candidates

Each generates 8 variations with subtle pitch/timbre randomization. Run with:
```bash
dotnet run --project AudioTest
```

### Recording your own

If you have a hot-swap board, record individual keypresses with:
- A condenser mic 6-12 inches from the board
- Room as quiet as possible
- Trim to just the keypress event (attack through tail)
- Normalize to -3 dBFS
- Export 44100 Hz / 16-bit mono WAV
- Aim for 8+ variations (different fingers, different keys)

---

## Adding a Soundscape

1. Create a directory under `Assets/Sounds/Ambient/` (e.g. `CoffeeShop/`)
2. Add one or more `.wav` files (any naming convention)
3. Build. The soundscape appears in the **Soundscape** dropdown automatically.

### Audio specs

| Property | Value |
|----------|-------|
| Sample rate | 44100 Hz recommended (MCI accepts others) |
| Bit depth | 16-bit PCM |
| Channels | Mono or Stereo |
| Duration | 30 seconds minimum, 2-5 minutes ideal |
| Loop point | Seamless loop preferred (crossfade tail into head) |

The engine loops the selected track with `mciSendString("play ... repeat")`.

### Current soundscapes

| Soundscape | Tracks | Character |
|------------|--------|-----------|
| **Ocean** | 3 | Gentle waves, pebble shore, coastal breeze |
| **Rain** | 3 | Steady rain, heavy downpour, thunderstorm |
| **Wind** | 2 | Alpine meadow, pine forest |
| **Zen** | 7 | Singing bowl, drones, pads, harmonics |

---

## Playback Behavior

- On startup, plays the first track (index 0) in the selected soundscape
- **Random button** — shuffles to a different track within the current soundscape
- **Mute button** — pauses/resumes the current track (does NOT restart)
- **Soundscape dropdown** — switches category and plays track 0 of the new soundscape
- **Keyboard theme dropdown** — switches SFX set immediately, no restart needed

---

## Where to Source Audio

| Source | License | Notes |
|--------|---------|-------|
| [Freesound.org](https://freesound.org) | CC0 / CC-BY | Search `seamless-loop`, `field-recording` |
| [BBC Sound Effects](https://sound-effects.bbcrewind.co.uk/) | RemArc license (personal/educational) | World-class fidelity |
| [Orange Free Sounds](https://orangefreesounds.com/) | CC-BY / Public Domain | Long-form nature recordings |
| [Kenney Assets](https://kenney.nl/assets?q=audio) | CC0 | Digital/UI audio packs |

### Keyboard Sound Repositories

| Project | What's in it |
|---------|-------------|
| **Mechvibes** | Community-contributed packs, folder-per-switch structure |
| **keyboard-sounds** (nathan-fiscaletti) | 15 bundled profiles + profile editor |
| **Daktilo** | CLI typewriter emulator, vintage + IBM buckling spring presets |

### Adapting external packs

Most external packs use different naming or structure. To import:

1. Copy the `.wav` files into a new `Sfx/ThemeName/` folder
2. Rename to `key_01.wav`, `key_02.wav`, etc.
3. Ensure 44100 Hz, 16-bit, mono (use `ffmpeg` to convert if needed):

```bash
ffmpeg -i input.wav -ar 44100 -ac 1 -sample_fmt s16 key_01.wav
```

---

## Future Enhancements

- **Multi-layer ambient playback** — Play 2-3 ambient tracks simultaneously with independent volumes
- **Per-key sound mapping** — Map specific keys (Space, Enter, Backspace) to distinct sounds
- **Live preview** — Play a sample keystroke when hovering over a theme in the dropdown
- **Soundscape crossfade** — Smooth transition when switching soundscapes mid-session
- **Generative ambient** — Use oscillators to synthesize infinite, non-repeating ambient textures
- **Hot-reload** — Watch the `Assets/Sounds/` directory for new folders and refresh dropdowns without restart
