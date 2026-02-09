# Migration: v0.1.0 → v0.2.0

## Persistence
- New schemaVersion = 2
- v1 data auto-migrates on load
- SessionHistory capped at 20 entries

## Snippets
- Existing packs continue to work
- Optional new fields:
  - length
  - symbolDensity
