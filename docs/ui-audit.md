# UI Audit — Screen-by-Screen Analysis

Date: 2026-03-03
Sprint: ui-polish (Issues #13–#17)

---

## Overview

Post-MVP visual audit of all four game screens. The terminal/CRT aesthetic is strong, but several areas suffer from text readability issues, and the tile board does not yet achieve the iconic Wheel of Fortune look. This document covers findings per screen and provides a reference for the associated GitHub issues.

---

## 1. Main Menu

**Current state:** SPELLWRIGHT title is prominent with amber-green gradient and glow. The START RUN button reads well with its amber accent.

**Issues found:**

| Element | Problem | Current Value |
|---------|---------|---------------|
| Subtitle | Barely visible or invisible — dim green on dark green bg | `Lerp(phosphorDim, phosphorGreen, 0.65)`, bodySize+2=28 |
| Hint text | "Press Enter or click to begin" too small and dim | smallSize+1=19, phosphorDim 75% alpha |
| Corner brackets | Too small to serve as visual frame | headerSize+8=38, placed at screen edges |
| Version text | Nearly invisible | smallSize=18, phosphorDim |
| Separators | Very thin and low contrast | smallSize=18-21, dim colors |
| Cursor blink | Tiny and easy to miss | bodySize=26, narrow anchor area |

**Recommendations:**
- Increase subtitle contrast (use phosphorGreen at full strength or a slightly brighter tint)
- Bump hint text to bodySize and increase alpha
- Scale up corner brackets (use decorativeTitleSize range)
- Add subtle glow to version text or use phosphorGreen at ~60% alpha
- Thicken separator lines or use higher alpha
- Consider adding a tagline or subtitle that persists after typewriter

---

## 2. Encounter — Tile Board (Critical)

**Reference:** Wheel of Fortune game board (see attached screenshot)

**Current implementation vs. WoF:**

| Aspect | Wheel of Fortune | Current Spellwright |
|--------|-----------------|-------------------|
| Layout | Fixed 4-row grid, uniform columns | Dynamic word-wrap, variable rows |
| Hidden tiles | Textured green tiles with decorative logo/pattern | Solid phosphorGreen rectangles |
| Revealed tiles | WHITE background, BLACK/dark text | Dark bg (panelBg), green text |
| Space tiles | Visible as blue/dark tiles in the grid | Invisible 18px gaps (no tile rendered) |
| Tile size | Large, prominent, fills the board area | 52x60px, relatively small |
| Board frame | Bold metallic/neon border, very prominent | Thin panelBg outline |
| Tile shape | Slightly portrait (taller than wide) | 52x60 (slightly portrait, good) |
| Category display | Below board in styled banner | Above board as text |

**Recommendations — "WoF Board Redesign":**

1. **Fixed grid layout:** Replace dynamic word-wrapping with a fixed grid (e.g., 3-4 rows x 12-14 columns). Words center within rows, unused positions are empty (no tile). This is the defining visual of WoF.

2. **Hidden tile texture:** Replace solid green fill with a textured tile — could be:
   - A procedural pattern (diamond/crosshatch) drawn via shader or RawImage
   - A small sprite with a Spellwright logo/rune pattern
   - A gradient green tile with a subtle decorative element

3. **Revealed tile colors:** Switch to WHITE/cream background with DARK text. This is the most recognizable WoF visual signature. The current green-on-dark scheme doesn't evoke WoF at all.
   - Background: near-white `(0.92, 0.95, 0.90)` or cream
   - Text: dark charcoal `(0.10, 0.10, 0.10)` or deep green `(0.05, 0.20, 0.10)`

4. **Space tiles visible:** Render spaces as distinct dark/blue tiles in the grid (like WoF's blue separators), OR leave grid positions empty but keep the grid structure visible.

5. **Larger tiles:** Scale up tiles to fill the board area. With a 14-column grid in the available width (~800px), tiles could be ~50-55px wide. Adjust tileHeight to ~60-65px.

6. **Board frame:** Add a more prominent border around the board area:
   - Double-line or thick outline with glow
   - Consider a neon-style border (cyan or amber glow)
   - Could add corner decorations

7. **Category banner:** Move category text to a styled banner below the board (matching WoF placement), with its own background and border.

---

## 3. Encounter — Text & Stats Readability

**Issues found:**

| Element | Problem | Current Value |
|---------|---------|---------------|
| History text | Too small, bottom of screen | smallSize+1=19, phosphorDim |
| Tome info text | Too small, low contrast | smallSize+1=19, magentaMagic |
| Guessed letters | Too small to track at a glance | smallSize=18, spacing=2 |
| Input mode text | "LETTER MODE" barely visible | smallSize+1=19, phosphorDim |
| NPC archetype | Hidden/unused | labelSize=22, hidden |
| Clue text | Could be more prominent | bodySize+4=30, phosphorGreen |
| Terminal prompt | Might blend into background | bodySize+2=28, phosphorGreen |

**Recommendations:**
- Bump history text to bodySize (26) range and increase contrast
- Increase tome info to labelSize (22+) range
- Enlarge guessed letters to labelSize or add bg tiles for each letter
- Make input mode text more visible (use bodySize, brighter color or subtle bg)
- Show NPC archetype below name at reasonable size
- Add a subtle background or border to the clue area for emphasis
- Consider reorganizing bottom section for better information hierarchy

---

## 4. Shop — Card Readability

**Issues found:**

| Element | Problem | Current Value |
|---------|---------|---------------|
| Card description | Too small to read comfortably | smallSize=18 |
| Price text | Could be more prominent | labelSize=22 |
| BUY/SELL labels | Very small on the card | smallSize=18 |
| Card height | Compressed, tight layout | 60px buy, 50px sell |
| Common rarity | Low contrast (green on dark green) | rarityCommon=(0.10,0.65,0.30) on cardBg |
| Tome effect text | Hard to parse at small size | Embedded in description |

**Recommendations:**
- Increase card height to 75-80px for buy items, 60px for sell
- Bump description text to labelSize (22) range
- Increase price text to bodySize (26) with bold/glow
- Make BUY/SELL labels larger (labelSize) and add padding
- Add a subtle brightness boost to common rarity color
- Consider two-line card content: name on first line, effect on second with different styling
- Add more vertical padding within cards for breathing room

---

## 5. Map Screen

**Issues found:**

| Element | Problem | Current Value |
|---------|---------|---------------|
| Future node labels | Too dim to read | nodeFuture=(0.2,0.45,0.3) at 65% alpha |
| Node status indicators | Small and easy to miss | smallSize=18 |
| Connector text | Dim and decorative | labelSize=22, same as node color |
| Stat chip text | Could be slightly larger | labelSize+2=24 |

**Recommendations:**
- Brighten future node text (use phosphorDim-to-phosphorGreen at ~50% lerp)
- Increase node status text or use iconographic symbols
- Consider making connector lines pure decoration (lower alpha) and node labels larger
- Bump stat chips to bodySize for easier scanning

---

## Priority Order

1. **Issue #14 — Tile Board Redesign** (highest impact — core gameplay screen)
2. **Issue #16 — Shop Card Readability** (second most time spent screen)
3. **Issue #15 — Encounter Text Readability** (complements #14)
4. **Issue #13 — Main Menu Readability** (first impression)
5. **Issue #17 — Map Screen Readability** (least severe)

---

## Reference Files

- Theme config: `Assets/Scripts/UI/TerminalThemeSO.cs`
- Scene builder: `Assets/Scripts/Editor/GameSceneSetup.cs`
- Tile board: `Assets/Scripts/Encounter/TileBoardUI.cs`
- Tile logic: `Assets/Scripts/Encounter/TileUI.cs`
- Shop UI: `Assets/Scripts/Shop/ShopUI.cs`
- Shop card: `Assets/Scripts/Shop/ShopCardUI.cs` (if exists) or `TerminalUIHelper.CreateItemCard`
- Main menu: `Assets/Scripts/Run/MainMenuUI.cs`
- Map: `Assets/Scripts/Run/MapUI.cs`
- Encounter: `Assets/Scripts/Encounter/EncounterUI.cs`
- Guessed letters: `Assets/Scripts/Encounter/GuessedLettersUI.cs`
