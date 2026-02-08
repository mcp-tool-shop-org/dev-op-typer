# Dev-Op-Typer — Product & Tech Spec (Starter+)

## Vision
An advanced-only typing trainer where **every test is real code**, full keyboard is utilized, and progression teaches real programming patterns.

## Core UX
- Single-window app (no tabs)
- Main stage: prompt (syntax-highlight ready), typing surface, live stats
- Right: collapsible Advanced Settings
- Persistent profile + settings

## Game Loop
1. Select language track (Python/Java/…)
2. App picks a snippet based on skill rating + weak symbols
3. Type exact-match (configurable whitespace strictness)
4. Earn XP and rating updates
5. Difficulty ramps: length, nesting, punctuation density, constructs

## Modes (Roadmap)
- Exact match (default)
- Debug drill (type corrected code)
- Fill-in-the-gap (type missing blocks)
- Refactor drill (type modified version with diff goals)

## Accessibility targets
- WCAG 2.2 AA
- Keyboard-only navigation
- Focus visible
- Reduced motion + reduced sensory audio
- High contrast theme option
