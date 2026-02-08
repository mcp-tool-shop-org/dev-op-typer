# Dev-Op-Typer v0.2.0 Planning

This directory contains the **design runway** for v0.2.0 — scaffolds, schemas, and documentation for the next release.

## Purpose

- **NOT** a replacement for v0.1.0 code
- **IS** a forward-looking feature scaffold
- Safe to reference without breaking the working app

## Contents

### Documentation
| File | Description |
|------|-------------|
| [v0.2.0-roadmap.md](v0.2.0-roadmap.md) | Design pillars and goals |
| [migration-v0.1.0-to-v0.2.0.md](migration-v0.1.0-to-v0.2.0.md) | Schema migration notes |
| [CHANGELOG-v0.2.0.md](CHANGELOG-v0.2.0.md) | Planned changes |

### Code Scaffolds (`scaffolds/`)
| File | Purpose |
|------|---------|
| `TypingRendererV2.cs` | Per-character rendering with symbol awareness |
| `MistakeHeatmap.cs` | Error tracking for adaptive difficulty |
| `SessionHistory.cs` | Capped session history (20 entries max) |

### Snippet Schema (`snippets/`)
| File | Purpose |
|------|---------|
| `schema-v2.json` | Extended snippet schema (length, symbolDensity) |
| `python_v2_sample.json` | Example advanced snippet |

## How to Use

1. **Reference, don't copy blindly** — read the roadmap first
2. **Implement incrementally** — one feature per commit
3. **Cherry-pick ideas** — adapt to existing patterns
4. **Keep v0.1.0 stable** — don't break what works

## v0.2.0 Pillars

1. **Visual precision** — per-character feedback
2. **Meaningful progression** — anti-grind XP curve
3. **Actionable learning** — mistake heatmaps
4. **Accessibility** — without reducing challenge
