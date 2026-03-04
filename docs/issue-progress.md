# Issue Progress
Last updated: 2026-03-04T03
Framework for UI: Unity UI Toolkit (UXML + USS) — per docs/ui-research.md

## Queue
| # | Title | Label | Status | Commit |
|---|-------|-------|--------|--------|
| 37 | UI Overhaul: Design North Star | ui | ⏭️ Epic tracker (skip) | — |
| 39 | Add CRT Audio Atmosphere | ui | ✅ Done | 902082f |
| 40 | Evaluate Authentic CRT Terminal Fonts | ui | ✅ Done | 7a3e014 |
| 41 | Create Terminal Design System (USS Theme) | ui | ✅ Done | ed21676 |
| 42 | UI Toolkit Migration: MainMenuUI (Pilot) | ui | ✅ Done | 2b2be4c |
| 43 | UI Toolkit Migration: MapUI (Journey Screen) | ui | ✅ Done | bfc5a1f |
| 44 | UI Toolkit Migration: ShopUI | ui | 🔄 In progress | — |
| 45 | UI Toolkit Migration: ResultUI | ui | ⏳ Queued | — |
| 46 | UI Toolkit Migration: EncounterUI | ui | ⏳ Queued | — |
| 48 | Remove Legacy uGUI Dependencies | ui | ⏳ Queued | — |
| 19 | AI Visibility: Design North Star | ai-visibility | ⏳ Queued | — |
| 20 | NPC Adaptive Difficulty (Mercy/Cruelty) | ai-visibility | ⏳ Queued | — |
| 21 | NPC Ultimatum (Endgame Showdown) | ai-visibility | ⏳ Queued | — |
| 22 | NPC Rival System (Persistent Antagonist) | ai-visibility | ⏳ Queued | — |
| 23 | Mood Bargain (Mid-Encounter Deal) | ai-visibility | ⏳ Queued | — |
| 24 | Letter Sacrifice (Strategic Tile Trade) | ai-visibility | ⏳ Queued | — |
| 25 | Journey Screen Redesign: Design North Star | journey | ⏳ Queued | — |
| 26 | ASCII Dungeon Map with Pipe-Connected Nodes | journey | ⏳ Queued | — |
| 27 | Expandable NPC Dossier Panels | journey | ⏳ Queued | — |
| 28 | Gold-for-Intel Economy | journey | ⏳ Queued | — |
| 29 | Pre-Encounter Gold Wagering | journey | ⏳ Queued | — |
| 30 | Dual-Pane Norton Commander Layout | journey | ⏳ Queued | — |
| 31 | Boss Wiretap — Progressive Intel | journey | ⏳ Queued | — |
| 32 | Tome Crucible — Sacrifice Two Tomes | journey | ⏳ Queued | — |
| 33 | Inline Shop as Terminal Overlay | journey | ⏳ Queued | — |
| 34 | Signal Strength Oscilloscope | journey | ⏳ Queued | — |
| 35 | Ghost Input Echo — Phosphor Atmosphere | journey | ⏳ Queued | — |
| 36 | Intercept Transmission Events & Dead Drops | journey | ⏳ Queued | — |

## Blocked
(none yet)

## Resume token
LAST_COMPLETED=43 | NEXT=44 | QUEUE_TOTAL=28

## Implementation Notes — #43
- Map.uxml: UXML layout with stats bar (wave/HP/gold/score chips), ScrollView node container, footer with lang toggle + proceed button
- map.uss: Comprehensive screen styles — node cards with state modifiers (completed/current/future/boss), color stripe classes, stat chip styling, breathing glow animation, entrance animations
- MapController.cs: UIDocument-based controller replacing uGUI MapUI — dynamic node creation via VisualElement/Label, staggered entrance with stagger-item/stagger-visible classes, breathing glow via schedule.Execute(), EventBus subscriptions for RunStarted/RunStateChanged
- GameSceneSetup: CreateMapPanel now creates UIDocument GO (like MainMenu), removed AddPanelAnimator for map; added `using Button = UnityEngine.UI.Button` and `using Image = UnityEngine.UI.Image` aliases to fix ambiguity with UIElements namespace
- DesignSystemTests: Added VerifyMapUIToolkit test (14 checks: 8 named elements, 2 Button types, 1 ScrollView, 1 CSS class, 2 asset loads)
- Old MapUI.cs NOT deleted yet (will be removed in #48 "Remove Legacy uGUI Dependencies")

## Implementation Notes — #42
- MainMenu.uxml: UXML layout with title, subtitle+cursor row, underline, separator, start button, hint, version
- main-menu.uss: Full screen-specific styles with staggered entrance animations (content slide, button scale, fade-in)
- MainMenuController.cs: UIDocument-based controller replacing uGUI MainMenuUI — uses USS transitions + schedule.Execute() for timing
- GameSceneSetup: CreateMainMenuPanel now creates root-level UIDocument GO instead of Canvas child
- EnsurePanelSettings: Creates shared PanelSettings asset with terminal-theme.tss at Assets/UI/SpellwrightPanelSettings.asset
- DesignSystemTests: Added VerifyMainMenuUIToolkit test (checks UXML elements, Button type, CSS classes, PanelSettings)
- Old MainMenuUI.cs NOT deleted yet (will be removed in #48 "Remove Legacy uGUI Dependencies")

## Implementation Notes — #41
- Created Assets/UI/ directory structure: Themes/, Styles/, Screens/
- variables.uss: All colors aligned with TerminalThemeSO.cs values (phosphor green palette, accents, states, components, HP bar, map nodes, rarity)
- reset.uss: Normalize defaults for UI Toolkit elements (buttons, inputs, scrollbars, toggles)
- typography.uss: Text classes (.text-title, .text-body, .text-stat, etc.) + color/alignment modifiers
- components.uss: Panel, button (with hover/active/disabled), card, input field, progress bar, tile cell, map node, rarity badge, tooltip
- layout.uss: Flexbox utilities (row/col, alignment, grow/shrink, spacing, display)
- animations.uss: USS transitions for panel entrance, fade, cursor blink, pulse, stagger, tile reveal, shake, scale pop, glitch
- terminal-theme.tss: Entry point TSS importing all 6 USS files
- 5 screen-specific USS stubs (main-menu, map, encounter, shop, result)
- theme-test.uxml: Visual test page showing all components, typography, colors, tiles, nodes, HP bars, rarity colors
- DesignSystemTests.cs: Editor menu test verifying all assets load correctly
