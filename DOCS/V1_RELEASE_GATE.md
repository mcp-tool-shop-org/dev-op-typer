# v1.0.0 Release Gate Checklist

Ship v1.0 when every box is checked.

## Trust & Determinism

- [ ] Guided Mode is opt-in (default OFF)
- [ ] With Guided Mode off, behavior matches v0.9 (tests prove)
- [ ] With Guided Mode on, only bounded WeaknessBias applies (tests prove)
- [ ] Golden end-to-end plan test passes (deterministic snapshot)
- [ ] SignalPolicy flags default to `false` for selection, difficulty, and XP

## Quality

- [ ] Migration tests cover prior schemas (no data loss)
- [ ] Perf gate passes at 5k CodeItems (planner + selection < 50ms)
- [ ] Signal retention prevents file growth runaway (heatmap + snapshot caps)
- [ ] All existing v0.9 tests continue to pass unmodified

## Accessibility

- [ ] Keyboard-only usable (no focus traps)
- [ ] Visible focus ring on all interactive elements
- [ ] High-contrast mode usable (no color-only indicators)
- [ ] Reduced-motion setting disables animations
- [ ] Debug inspector and Guided Mode toggle fully keyboard-accessible

## Product Clarity

- [ ] First-run flow is coherent (explains Guided Mode, shows how to add code)
- [ ] "Add code" flow is obvious and discoverable
- [ ] Library health panel exists (item count, languages, last index time)
- [ ] Docs explain what signals do and don't do (trust doc)
- [ ] CHANGELOG entry for v1.0.0 is complete

## Guided Mode Stance (Locked)

| Property        | Value                          |
|-----------------|--------------------------------|
| Default         | OFF                            |
| User-controlled | Explicit opt-in                |
| Persistence     | Remembers user choice          |
| Reversibility   | Can be toggled at any time     |

This preserves: determinism, predictability, user agency.

## Invariant Guarantee

> With Guided Mode off, planner output is byte-for-byte identical to v0.9 snapshot.
