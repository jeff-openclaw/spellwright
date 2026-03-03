# Encounter UI Polish — Iterative Task

## Status: Complete

## Goal
Polish the encounter screen layout to achieve a clean, Wheel-of-Fortune-inspired aesthetic with tight spacing and clear visual hierarchy.

## Current Issues & Fixes

### 1. WoF-Style Fixed Grid Board
- **Problem**: Board only shows tiles for the exact word letters — looks sparse for short words.
- **Target**: Fixed grid of tile slots (like WoF) where the word fills some and the rest are empty/dim spare boxes.
- **Approach**: Calculate grid cols from container width, always show 2+ rows, pad with empty (dark, subtle) tiles.
- **Status**: Done

### 2. Category Banner — Pixel-Perfect Fit
- **Problem**: Gap between board frame bottom and dialog card top; banner floats with visible margins.
- **Target**: Banner fills the full horizontal width (matching board/dialog) and sits flush vertically between them.
- **Anchors**: (0.04, 0.66) to (0.96, 0.73) — same width as board frame, flush top and bottom.
- **Status**: Done

### 3. Category Text — Clean Display
- **Problem**: Shows `── Category: nature` with prefix and spinner decoration.
- **Target**: Just the category name in ALL CAPS, no prefix (e.g., `NATURE`).
- **Status**: Done

### 4. Archetype Subtitle — Remove
- **Problem**: "Riddlemaster" subtitle still shows below the NPC name.
- **Target**: Completely hidden — kept only for wiring at zero visible size.
- **Status**: Done

### 5. NPC Name — Bottom-Aligned with Portrait
- **Problem**: Name sits above the portrait vertically; looks disconnected.
- **Target**: Name and portrait share the same row; name baseline aligns with portrait bottom.
- **Status**: Done

## Layout Reference (Encounter Panel, top to bottom)

```
Board Frame:      (0.04, 0.73) — (0.96, 0.97)   [tile grid inside]
Category Banner:  (0.04, 0.66) — (0.96, 0.73)   [flush between board & dialog]
Dialog Card BG:   (0.04, 0.40) — (0.96, 0.66)
  Portrait:       (0.05, 0.55) — (0.16, 0.65)
  NPC Name:       (0.17, 0.55) — (0.60, 0.60)   [bottom-aligned with portrait]
  Separator:      (0.05, 0.53) — (0.95, 0.54)
  Clue #:         (0.06, 0.49) — (0.30, 0.53)
  Clue Text:      (0.06, 0.41) — (0.94, 0.49)
Stats Bar:        (0.04, 0.34) — (0.96, 0.40)
```

## Files Modified
- `Assets/Scripts/Encounter/TileBoardUI.cs` — WoF fixed grid
- `Assets/Scripts/Encounter/EncounterUI.cs` — category uppercase, no prefix
- `Assets/Scripts/UI/TextSpinner.cs` — default prefix changed
- `Assets/Scripts/Editor/GameSceneSetup.cs` — layout positions
