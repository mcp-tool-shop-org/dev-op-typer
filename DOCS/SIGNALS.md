# Signals vs Engines: What They Do and Don't Do

## What are signals?

Signals are data the app collects as you practice:

- **Mistake heatmap**: which characters you mistype most often
- **Weakness categories**: symbol groups where you struggle (brackets, operators, quotes, etc.)
- **Trajectory**: whether each weakness is improving, worsening, steady, or new
- **Difficulty memory**: which difficulty level feels comfortable for each language

Signals are always collected, even with Guided Mode off. They power the stats panel and debug inspector.

## What do signals NOT do?

Signals never:

- Change your rating or XP
- Force you into a specific difficulty band
- Lock you out of any content
- Penalize you for mistakes
- Auto-enable any behavior change

## What does Guided Mode change?

When you enable Guided Mode (off by default), exactly one thing changes:

> Within the difficulty band you're already playing at, the app slightly prefers snippets that exercise your weak symbol groups.

That's it. Specifically:

- **Band stays the same**: if you're at D4, you stay at D4
- **Scoring stays the same**: your WPM, accuracy, and XP are unaffected
- **Bias is bounded**: the preference is a nudge (+15 max), not a redirect
- **Diversity is protected**: the app needs 2+ weak groups before activating
- **Randomness remains**: you'll still see a variety of snippets

## What Guided Mode does NOT change

| Aspect | With Guided Mode OFF | With Guided Mode ON |
|--------|---------------------|---------------------|
| Difficulty band | Unchanged | Unchanged |
| Rating/XP | Unchanged | Unchanged |
| Snippet pool | Full library | Full library |
| Language selection | Your choice | Your choice |
| Scoring formula | Standard | Standard |
| Snippet ordering | Standard | Slightly biased toward weaknesses |

## How to verify

1. Press **Shift+F12** to open the debug inspector
2. Look at the "Plan" section — it shows exactly why each snippet was chosen
3. Toggle Guided Mode off and on — the pick reason will update
4. With Guided Mode off, the app behaves identically to v0.9

## Privacy

All signal data is stored locally on your machine. Nothing is sent anywhere.
