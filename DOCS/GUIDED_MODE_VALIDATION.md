# Guided Mode — Manual Validation Checklist

Run through this checklist before any release that touches signal or selection code.
Every item must pass with the exact outcome described.

## Prerequisites

- [ ] Fresh install or reset (`PersistenceService.Reset()`)
- [ ] At least 10 built-in snippets available in the current language
- [ ] Debug inspector accessible via Shift+F12 (Debug builds)

---

## 1. Default State — Guided Mode OFF

| # | Step | Expected |
|---|------|----------|
| 1.1 | Launch app, open Settings panel | Guided Mode toggle is OFF |
| 1.2 | Complete 3 practice sessions | Pick reasons show standard selection (no "weakness" reasons) |
| 1.3 | Open debug inspector (Shift+F12) | SignalPolicy shows `GuidedMode=false`, `EffectiveSelectionBias=false` |
| 1.4 | Deliberately mistype `{` and `}` 20 times each | Heatmap records misses, but selection is unchanged |
| 1.5 | Complete 3 more sessions | Snippet selection shows no weakness bias influence |

**Pass criteria**: With Guided Mode OFF, selection is byte-for-byte identical to v0.9 behavior.

---

## 2. Enabling Guided Mode

| # | Step | Expected |
|---|------|----------|
| 2.1 | Open Settings, toggle Guided Mode ON | Toggle shows ON state |
| 2.2 | Open debug inspector | `GuidedMode=true`, `SignalsAffectSelection=true`, `EffectiveSelectionBias=true` |
| 2.3 | `SignalsAffectDifficulty` and `SignalsAffectXP` | Both remain `false` |
| 2.4 | Complete 3 sessions | Pick reasons may show weakness-biased selections |
| 2.5 | Check difficulty band | Unchanged from pre-toggle value |
| 2.6 | Check XP calculation | Unchanged from standard formula |

**Pass criteria**: Only selection bias activates; difficulty and XP are never affected.

---

## 3. Bias is Bounded

| # | Step | Expected |
|---|------|----------|
| 3.1 | With Guided Mode ON, mistype `{`, `}`, `(`, `)` heavily | WeaknessBias applied to selections |
| 3.2 | Check debug inspector scoring | `WeaknessBias.ComputeCategoryBias` ≤ 15.0 for any snippet |
| 3.3 | Complete 10 sessions | Still see variety of snippets (not all bracket-heavy) |
| 3.4 | Only 1 weak symbol group (e.g., only brackets) | Diversity guard: bias shows 0 (needs 2+ weak groups) |

**Pass criteria**: Bias is capped at +15, diversity guard requires 2+ weak groups.

---

## 4. Persistence

| # | Step | Expected |
|---|------|----------|
| 4.1 | Toggle Guided Mode ON, close app | — |
| 4.2 | Reopen app, check Settings | Guided Mode is still ON |
| 4.3 | Toggle OFF, close app | — |
| 4.4 | Reopen app, check Settings | Guided Mode is still OFF |
| 4.5 | Check debug inspector after restart | SignalPolicy matches persisted state |

**Pass criteria**: User's Guided Mode choice survives app restart.

---

## 5. Heatmap Data Integrity

| # | Step | Expected |
|---|------|----------|
| 5.1 | Complete 5 sessions with varied mistakes | Heatmap shows per-char error rates |
| 5.2 | Close and reopen app | Heatmap data preserved |
| 5.3 | Check heatmap record count | ≤ 200 (MaxTrackedChars) |
| 5.4 | Check confusion pairs per char | ≤ 20 (MaxConfusionPairs) |
| 5.5 | Corrupt heatmap data manually | App loads with empty heatmap (graceful recovery) |

**Pass criteria**: Heatmap persists, prunes to bounded size, recovers from corruption.

---

## 6. Micro-Drill Flow

| # | Step | Expected |
|---|------|----------|
| 6.1 | With Guided Mode ON and weakness data | Micro-drill generates 5 snippets |
| 6.2 | Drill snippets target top weakness | Drill description mentions weak symbol group |
| 6.3 | No duplicate snippets in drill | All 5 snippets have unique IDs |
| 6.4 | Drill completes normally | Session records written with correct pick reasons |

**Pass criteria**: Micro-drills focus on weaknesses without duplicates.

---

## 7. Edge Cases

| # | Step | Expected |
|---|------|----------|
| 7.1 | New user, no heatmap data, Guided Mode ON | Standard selection (no crash, no bias) |
| 7.2 | Single snippet in library | Selection works, no infinite loop |
| 7.3 | Switch language mid-session | Plan regenerates for new language |
| 7.4 | Rapid toggle ON/OFF/ON | State is consistent, no stale bias |

---

## Sign-off

| Version | Tester | Date | Result |
|---------|--------|------|--------|
| 1.0.0-dev | | | |
