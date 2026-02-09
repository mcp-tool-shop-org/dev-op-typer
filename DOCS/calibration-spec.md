# Calibration Corpus Specification — v0.9.0

## Purpose

Calibration content exists to teach the engine, not the user. It provides ground truth: a known set of snippets with predictable difficulty bands, allowing the session planner to make intelligent selection choices from day one.

## Target Languages (Initial Set)

1. **Python** — dynamic, significant whitespace, low symbol density
2. **JavaScript** — curly-brace, moderate density, template literals
3. **C#** — curly-brace, generics, LINQ operators
4. **Java** — verbose, generics, checked exceptions
5. **Bash** — pipeline operators, variable expansion, quoting
6. **SQL** — keyword-heavy, low nesting, aggregate patterns

Additional languages may be added in future versions.

## Difficulty Band Coverage

Each language pack must cover all 7 difficulty bands with minimum item counts:

| Band | Min Items | Typical Characteristics |
|------|-----------|------------------------|
| D1   | 5         | 1-5 lines, low density, flat |
| D2   | 5         | 6-10 lines, low density, shallow nesting |
| D3   | 5         | 8-15 lines, moderate density, 1-2 indent levels |
| D4   | 5         | 12-20 lines, moderate density, 2-3 indent levels |
| D5   | 5         | 15-25 lines, higher density, 3 indent levels |
| D6   | 5         | 20-30 lines, high density, 3-4 indent levels |
| D7   | 3         | 25+ lines, high density, 4+ indent levels |

Minimum per language: 33 items. Target: 35-50.

## File & Folder Convention

```
DevOpTyper/Assets/Calibration/
  python.json
  javascript.json
  csharp.json
  java.json
  bash.json
  sql.json
```

Each file is a JSON array of `Snippet` objects following the existing schema:

```json
[
  {
    "Id": "cal-py-d1-001",
    "Language": "python",
    "Difficulty": 1,
    "Title": "Simple assignment",
    "Code": "x = 42\n",
    "Topics": ["assignment"],
    "Explain": ["Assigns integer 42 to variable x"]
  }
]
```

### Naming Convention

- **Id**: `cal-{lang_prefix}-d{band}-{seq}` (e.g., `cal-py-d3-007`)
- **Title**: descriptive, lowercase, no articles

### Language Prefixes

| Language | Prefix |
|----------|--------|
| python | py |
| javascript | js |
| csharp | cs |
| java | jv |
| bash | sh |
| sql | sq |

## Calibration vs Ship-to-Users

| Aspect | Calibration | User Content |
|--------|-------------|--------------|
| Purpose | Engine ground truth | Practice material |
| Origin tag | `calibration-pack-v1` | `builtin` or `user` |
| Source tag | `calibration` | `builtin` or `user` |
| Editable | No | User content is editable |
| Shown in stats | Configurable (default: excluded) | Always included |
| Difficulty | Authored and verified | Authored or derived |
| Selection | Planner-driven | Rating + weakness driven |

## Ingestion

Calibration packs are ingested into the `ContentLibraryService` with:
- `source = "calibration"`
- `origin = "calibration-pack-v1"`

The service filters calibration content out of `GetSnippets()` by default. A separate `GetCalibrationSnippets()` method exposes them for the session planner.

## Invariants

1. Every band 1-7 has at least the minimum item count per language.
2. Calibration items' derived difficulty (from `DifficultyEstimator`) lands in the authored band ±1.
3. Calibration IDs are stable across versions (content-addressed).
4. Calibration content is never modified by user actions.
5. Removing calibration packs does not corrupt the content library.
