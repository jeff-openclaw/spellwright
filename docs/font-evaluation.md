# Oldschool PC Font Evaluation

## Source
**Ultimate Oldschool PC Font Pack v2.2** by VileR
- https://int10h.org/oldschool-pc-fonts/
- License: CC BY-SA 4.0 (credit: "VileR, The Ultimate Oldschool PC Font Pack")
- 200+ TrueType fonts from real IBM PCs, VGA/EGA/CGA/MDA adapters

## Candidates Evaluated

All fonts use the "PxPlus" variant (782 chars — Latin, Greek, Cyrillic, box drawing).
All 6 support **full Romanian diacritics** (ă, â, î, ș, ț, Ă, Â, Î, Ș, Ț) ✓

| # | Font | Source Hardware | Character | SDF Generated |
|---|------|----------------|-----------|---------------|
| 1 | PxPlus_IBM_VGA_8x16 | IBM PS/2, VGA standard | **THE** classic DOS look | ✓ |
| 2 | PxPlus_IBM_VGA_9x16 | IBM VGA (9-dot mode) | Slightly wider VGA, extra column | ✓ |
| 3 | PxPlus_IBM_EGA_8x14 | IBM EGA adapter | Shorter, earlier era | ✓ |
| 4 | PxPlus_IBM_CGA | IBM CGA adapter | Chunkiest, most retro | ✓ |
| 5 | PxPlus_IBM_MDA | IBM Monochrome Display | THE green phosphor font | ✓ |
| 6 | PxPlus_Amstrad_PC | Amstrad PC1512/1640 | European terminal feel | ✓ |

## SDF Assets

Generated via `Spellwright > Fonts > Generate All SDF Assets`:
- Location: `Assets/Fonts/OldschoolPC/SDF/`
- Sampling: 32pt, SDFAA render mode, 1024x1024 atlas
- Character set: ASCII + Romanian + European + Box Drawing (░▒▓│┤╡... etc.)

## Comparison vs VT323

| Aspect | VT323 | Oldschool PC Fonts |
|--------|-------|--------------------|
| Origin | Google Font, inspired by DEC VT320 | Pixel-perfect from real IBM hardware |
| Authenticity | "Inspired by" | "The actual thing" |
| Style | Clean, modern retro | Raw, chunky pixel art |
| Romanian | ✓ | ✓ (PxPlus variants) |
| Best use | Body text, readability | Tile board, headers, terminal prompt |

## Decision

**Adopt PxPlus_IBM_VGA_8x16 as the primary decorative/display font.**

Rationale:
- Most recognizable DOS terminal look — instant nostalgia
- 8x16 pixel grid renders cleanly at game resolution
- Full Romanian + box drawing character support
- VGA was the dominant standard — players will recognize it subconsciously

**Keep VT323 as the body/readability font** for longer text passages (clues, descriptions).

**PxPlus_IBM_MDA is the runner-up** — literally THE green monochrome display font. Consider it for the tile board letters specifically if you want maximum CRT authenticity.

### Where to use each font

| Element | Font | Reason |
|---------|------|--------|
| Tile board letters | PxPlus_IBM_VGA_8x16 | Large, single chars — pixel look shines |
| Terminal prompt | PxPlus_IBM_VGA_8x16 | DOS command-line feel |
| Stats/HUD | PxPlus_IBM_VGA_8x16 | Compact, clear numbers |
| Button labels | PxPlus_IBM_VGA_8x16 | Terminal button aesthetic |
| Clue text (body) | VT323 | Longer text, needs readability |
| NPC dialogue | VT323 | Readability over style |

### How to swap

In Unity Inspector: change `TerminalThemeSO.decorativeFont` to the desired SDF asset from `Assets/Fonts/OldschoolPC/SDF/`. Use `Spellwright > Fonts > Log Font Comparison Info` for a quick reference.

## Attribution (required by CC BY-SA 4.0)

> Fonts by VileR, The Ultimate Oldschool PC Font Pack
> https://int10h.org/oldschool-pc-fonts/
