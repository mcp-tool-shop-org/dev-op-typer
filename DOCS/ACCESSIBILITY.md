# Accessibility Checklist

## Keyboard & focus
- All interactive controls reachable with Tab
- Logical tab order (top bar → main → sidebar)
- Strong visible focus indicator

## Screen readers
- Avoid announcing each character typed
- Provide occasional status updates (optional): WPM, accuracy
- Controls have clear names and states (ARIA-like via AutomationProperties)

## Visual
- High contrast palette
- Font size scaling
- Avoid relying only on color for errors (underline / icons)

## Motion & sound
- Respect OS 'reduce motion'
- Provide 'reduced sensory' mode (lower volumes, fewer SFX)
