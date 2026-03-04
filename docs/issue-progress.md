# Issue Progress
Last updated: 2026-03-04T05
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
| 44 | UI Toolkit Migration: ShopUI | ui | ✅ Done | f8afedb |
| 45 | UI Toolkit Migration: ResultUI | ui | ✅ Done | b1068aa |
| 46 | UI Toolkit Migration: EncounterUI | ui | ✅ Done | 96671bb |
| 48 | Remove Legacy uGUI Dependencies | ui | ✅ Done | 927b8bd |
| 19 | AI Visibility: Design North Star | ai-visibility | ⏭️ Epic tracker (skip) | — |
| 20 | NPC Adaptive Difficulty (Mercy/Cruelty) | ai-visibility | ✅ Done | 3513835 |
| 21 | NPC Ultimatum (Endgame Showdown) | ai-visibility | ✅ Done | 5d3ac65 |
| 22 | NPC Rival System (Persistent Antagonist) | ai-visibility | ✅ Done | 0c51da3 |
| 23 | Mood Bargain (Mid-Encounter Deal) | ai-visibility | ✅ Done | b07ef2d |
| 24 | Letter Sacrifice (Strategic Tile Trade) | ai-visibility | ✅ Done | b8cdb6b |
| 25 | Journey Screen Redesign: Design North Star | journey | ⏭️ Epic tracker (skip) | — |
| 26 | ASCII Dungeon Map with Pipe-Connected Nodes | journey | ✅ Done | ee069ac |
| 27 | Expandable NPC Dossier Panels | journey | ✅ Done | b10b5c0 |
| 28 | Gold-for-Intel Economy | journey | ✅ Done | 95125d0 |
| 29 | Pre-Encounter Gold Wagering | journey | ✅ Done | c17cb7c |
| 30 | Dual-Pane Norton Commander Layout | journey | ✅ Done | 2897074 |
| 31 | Boss Wiretap — Progressive Intel | journey | ✅ Done | 5052a28 |
| 32 | Tome Crucible — Sacrifice Two Tomes | journey | ✅ Done | e4e85d4 |
| 33 | Inline Shop as Terminal Overlay | journey | ✅ Done | 53a97b5 |
| 34 | Signal Strength Oscilloscope | journey | ✅ Done | d15a54b |
| 35 | Ghost Input Echo — Phosphor Atmosphere | journey | ✅ Done | PENDING |
| 36 | Intercept Transmission Events & Dead Drops | journey | ⏳ Queued | — |

## Blocked
(none yet)

## Resume token
LAST_COMPLETED=35 | NEXT=36 | QUEUE_TOTAL=28

## Implementation Notes — #35
- Added GhostLetter struct and GhostLetters list to RunManager — tracks (Letter, Correct) from all guesses via GuessSubmittedEvent subscription
- Letter guesses record single chars; phrase guesses record all letters from the guessed word
- Ghost pool of 10 absolutely-positioned Labels on map root, recycled round-robin
- Spawns one ghost every ~800ms at random screen position (5-90% x, 5-85% y)
- Correct guesses: brighter green (0.25 opacity), linger 1.5s before 400ms fade
- Wrong guesses: dim amber (0.15 opacity), fast 600ms + 400ms fade
- Suppressed during shop overlay open or dossier expansion (no visual competition)
- Ghost letters cleared on StartRun, pool cleaned up on StopGhostEcho/OnDisable
- USS: absolute positioned, large font size, transition on opacity, correct/wrong color classes

## Implementation Notes — #34
- Added SignalLabel to each dungeon node row between file permissions and outcome text
- 6-character wide oscilloscope waveform using Unicode wave chars (∿ ∾ ~ for clean, ≋ ⌇ ≈ ≡ for noisy)
- Waveform animates continuously at 200ms intervals via scheduled callback
- Difficulty-to-noise mapping: easy (0.1 noise) → smooth sine waves, hard (0.6-0.8) → erratic with scanline tears, boss (0.9) → maximally unstable
- Completed nodes: flatline with ─ characters at 50% opacity
- Color coding: green (active/easy), amber (noisy/hard), red (boss), dim (flatline/completed)
- GenerateWaveform() uses sine wave + seeded RNG noise injection, with glitch character substitution at high noise levels
- GetSignalNoise() reads NPC difficultyModifier via GameManager.PreviewNPCForNode()
- USS styles: min-width 48px, letter-spacing 1px, state classes for active/noisy/boss/flat

## Implementation Notes — #33
- Replaced separate shop screen flow with inline terminal popup overlay on the map screen
- Added shop overlay UXML: centered popup with bordered panel containing TOMES FOR SALE, SERVICES (heal + sell), gold display, feedback, and close button
- Added [TAB] SHOP button to map status bar for manual shop access at any time
- MapController now holds ShopManager reference and builds shop items dynamically: CreateShopBuyItem (buy tomes), BuildShopHealButton (heal service), BuildShopSellItems (sell equipped tomes)
- Shop overlay show/hide: OpenShop() generates inventory + refreshes, CloseShop() hides + refreshes map stats. Backdrop click or close button dismisses
- Flow change: GameManager.GoToShop() now calls ReturnToMapWithShop() which advances the node, flags MapController.RequestShopAutoOpen(), and transitions to Map. Shop popup auto-opens 300ms after map renders
- EncounterController.OnContinueClicked() still calls GoToShop() — no change needed there
- Gold updates in real-time in both shop popup and status bar via UpdateShopGold() + UpdateStats()
- USS: overlay backdrop (60% black), popup panel with amber border, buy items with rarity colors, sell items, heal button, slide-in animation
- GameSceneSetup: wires ShopManager to MapController in addition to ShopController

## Implementation Notes — #32
- Created TomeCrucible.cs: singleton MonoBehaviour, FuseTomes(idA, idB) removes both input tomes and equips a fused tome with the rarer effect and bumped rarity. Limited to one fusion per wave via _lastFusionWave tracking
- Fused tome naming: "{PrimaryTomeName} †" with upgraded rarity (capped at Legendary)
- Fusion picks the rarer tome's effect to preserve, creating a stronger version of the better input
- Added CrucibleFusedEvent to GameDataModels (InputA, InputB, Result TomeInstances)
- Extended MapController.RefreshTomeLoadout() with BuildCrucibleUI(): shows "> CRUCIBLE" section with clickable tome buttons. Two-click selection flow: first click selects (magenta highlight), second click on different tome triggers fusion
- OnCrucibleTomeClicked handles selection state, calls TomeCrucible.FuseTomes, shows result label, auto-refreshes after 1.5s delay
- Added crucible USS: header (magenta), locked state, option buttons with hover/selected states, result text
- Updated GameSceneSetup: creates TomeCrucible GameObject

## Implementation Notes — #31
- Enhanced RefreshBossWiretap() to use actual boss NPC data: PreviewNPCForNode for boss name, PreviewCategoryForNode for category, difficultyModifier for threat level, bossConstraint for tactic
- 5 progressive fragments: (1) redacted boss name (20% → 60% → 100% visible), (2) garbled category (clears at fragment 4), (3) threat level bar, (4) boss tactic/constraint, (5) defeat warning + READY status
- Added decrypt animation: new fragments scramble through random glitch chars (!@#$%^&*<>{}[]) over 640ms (8 cycles at 80ms), progressively settling on target text via ScramblePartial()
- Animation tracked via _lastWiretapFragments field — only triggers when fragment count increases between refreshes
- Boss node dossier (#27) already syncs via shared encountersWon/5 legibility formula — no changes needed
- Added ScrambleText() and ScramblePartial() helper methods for character-cycling effect

## Implementation Notes — #30
- Rewrote Map.uxml: dual-pane layout with left pane (Encounter Directory + ScrollView) and right pane (System Log with Run Log, Tome Loadout, Boss Wiretap sections). Bottom status bar spans full width with HP/gold/score/rival + wave/proceed/lang-toggle
- Rewrote map.uss: dual-pane flexbox (50/50 split), pane headers with dim background, pane divider with border, section headers (cyan), log/tome/wiretap entry styles, status bar with bottom border
- File-listing metaphor: nodes display as room_01.enc/boss.enc.??? with rwx/r--/---/??? permissions
- Modified MapController: removed left/right pipe chars from nodes, added right pane elements (_runLogContainer, _tomeLoadoutContainer, _bossWiretapContainer), RefreshRightPane() called on map refresh and state change
- RefreshRunLog: shows encounter outcomes as ENC_XX RC=0/1 with gold earned, win/loss color coding
- RefreshTomeLoadout: shows equipped tomes with slot numbers and [ACT] status, empty slots as (empty)
- RefreshBossWiretap: progress bar (▓░), fragment counter [X/5 FRAGMENTS], progressive intel lines unlocked per encounter won (boss clue style, difficulty, defeat consequence, constraint, full intel)

## Implementation Notes — #29
- Added wager tier config to GameConfigSO: wagerCosts[0,5,10,20], wagerMultipliers[1.0,1.5,2.0,3.0], wagerDamageBonus[0,1,2,3]
- Added WagerConfirmedEvent to GameDataModels
- Added RunManager wager state: _currentWager, _wagerMultiplier, _wagerDamageBonus with PlaceWager(tierIndex) and ClearWager(). Properties exposed for EncounterManager/EncounterController
- Modified RunManager.OnEncounterEnded: reward multiplied by _wagerMultiplier, staked gold returned on win (lost on defeat), ClearWager() after each encounter
- Modified EncounterManager.ApplyHPLoss: adds WagerDamageBonus to base HP loss before bargain/tome modifiers
- Added MapController.AddWagerSection(): tier buttons in current node's dossier with cost/multiplier/damage labels, OnWagerClicked() with selected state highlighting
- Added wager USS: wager-separator (amber), wager-container (flex row), wager-btn (amber border, safe/disabled/selected variants)
- Added wager chip to Encounter.uxml stats bar: shows "STAKE: Xg (Y.Zx)" during encounter
- Added EncounterController.UpdateWagerDisplay(): shows/hides wager chip based on RunManager.CurrentWager
- Added encounter.uss: stat-chip--wager style (amber)

## Implementation Notes — #28
- Added IntelType enum (WordLength, FirstLetter, Weakness) and NodeIntelData class to GameDataModels, plus IntelUnlockedEvent
- Added intel cost config to GameConfigSO: Vector3Int per intel type (easy/mid/hard tiers), GetIntelCost() helper method. Costs: WordLength (3/5/8g), FirstLetter (5/8/12g), Weakness (8/10/15g)
- Added RunManager intel tracking: _nodeIntel dictionary, GenerateIntel() called by GameManager at wave start, GenerateNodeIntel() pre-picks representative words from pools using seeded RNG, TryUnlockIntel() handles gold spending + event publishing
- Added GenerateWeaknessHint() providing archetype-specific tactical advice
- Extended MapController.BuildRegularDossier(): adds purchasable intel lines after free dossier info, AddIntelLine() creates locked/unlocked rows with cost buttons, OnIntelUnlockClicked() handles purchase + immediate visual update
- Added USS: intel-separator, intel-row (flex row), intel-text (locked dim / unlocked green), intel-btn (amber border, disabled state, unlocked state)
- GameManager calls RunManager.GenerateIntel(ActiveWordPools) in both RunSetup and boss victory (StartNextWave) paths
- Boss nodes excluded from intel generation (classified theme from #27)

## Implementation Notes — #27
- Added PreviewNPCForNode(int nodeIndex, NodeType nodeType) and PreviewCategoryForNode(int nodeIndex) to GameManager for map dossier preview without affecting encounter flow
- Extended MapController with expandable dossier panels: ToggleDossier() click handler, BuildDossierContent() dispatches to BuildRegularDossier() or BuildBossDossier(), CollapseAllDossiers() on map refresh
- Regular dossier: redacted NPC name (RedactText 40% visible), threat level bar (▓░ 1-5 scale), garbled category (GarbleText 50% corrupted)
- Boss dossier: classified state when <1 encounter won, progressive legibility (name reveal, threat bar, caution text) based on encountersWon/5 fraction
- Added dossier USS styles: expandable panel with opacity+max-height transition, line color modifiers (subject/threat/category/classified), separator with box-drawing character
- Nodes are clickable (expandable cursor hint class), completed nodes skip dossier expand, accordion behavior (only one dossier open at a time)

## Implementation Notes — #26
- Completely redesigned Map.uxml: ASCII box-drawing frame (╔═╗║╚═╝╠╣) wrapping status bars and dungeon nodes. Stats rendered inline in frame rows (WAVE, HP block bar, GOLD, SCORE, RIVAL). Node container as ScrollView inside frame
- Rewrote map.uss: dungeon-row layout with pipe characters (║) on sides, node state modifiers (completed/current/future/boss/current-boss), HP block bar with █/░ characters, stagger entrance animations (slide-in from left), breathing glow for current node
- Rewrote MapController.cs: CreateDungeonNode builds row with left pipe + node content (indicator + room label + outcome) + right pipe. AddPipeConnector creates vertical pipe between nodes. UpdateNodeStates uses [✓]/[▶]/[ ]/[☠] indicators, shows outcome summaries ("SOLVED 4/6 +12g") for completed nodes using RunManager.NodeOutcomes. BuildHPBar renders █░ block bar. Stagger animation targets Row elements
- Modified RunManager: added NodeOutcome struct (NodeIndex, Won, GuessCount, GoldEarned) and _nodeOutcomes list. Populated in OnEncounterEnded, cleared on StartRun. Exposed as IReadOnlyList<NodeOutcome> NodeOutcomes property

## Implementation Notes — #24
- Created LetterSacrificeSystem.cs: MonoBehaviour tracking sacrifice state (used/active), toggles sacrifice mode, calls BoardState.RehideTile on tile click, publishes LetterSacrificedEvent and SacrificeModeToggledEvent. Limited to one sacrifice per encounter
- Added BoardState.RehideTile(int index): sets revealed tile back to Hidden state, returns the character
- Added LetterSacrificedEvent + SacrificeModeToggledEvent to GameDataModels
- Modified PromptBuilder.BuildCluePrompt/BuildUserMessage: added optional sacrificedLetter param, injects sacrifice context ("The player has SACRIFICED a revealed letter...") into LLM prompt
- Modified LLMManager.GenerateClueAsync/TryLLMClueAsync: pass through sacrificedLetter param to PromptBuilder
- Modified EncounterManager: subscribes to LetterSacrificedEvent, RequestSacrificeClue() generates sacrifice-context clue (no guess consumed, no letter reveals)
- Added sacrifice-btn to Encounter.uxml in new input-controls row alongside input-mode label
- Added encounter.uss: sacrifice button styles (magenta, active/spent states), sacrifice-target tile highlight, shatter animation (scale-to-zero + opacity)
- Modified EncounterController: sacrifice toggle button, SacrificeModeToggledEvent handler with target tile highlights and ClickEvent registration, LetterSacrificedEvent handler with shatter animation + spent state, UpdateSacrificeButtonVisibility (shows after 3+ tiles revealed)
- Updated GameSceneSetup: creates LetterSacrificeSystem GameObject

## Implementation Notes — #23
- Created MoodBargainSystem.cs: subscribes to ClueReceivedEvent, detects mood changes, offers time-limited bargains (8s) based on NPC archetype + mood. Bargain table: Guide (generous: free vowel/heal), Riddlemaster (fair: vowel-for-guess/double-stakes), TricksterMerchant (risky: vowel-for-guess/double-stakes), SilentLibrarian (cryptic: vowel/skip-guess). Timer runs in Update(), publishes BargainExpiredEvent on timeout
- Added BargainEffect enum (RevealVowel, SkipGuess, DoubleRisk, HealSmall) + BargainOfferedEvent, BargainAcceptedEvent, BargainExpiredEvent to GameDataModels
- Added BoardState.RevealRandomVowel(): reveals single random hidden vowel tile, returns index or -1
- Modified EncounterManager: subscribes to BargainAcceptedEvent, handles SkipGuess (consumes guess slot), handles DoubleRisk (2× HP loss on next wrong guess via MoodBargainSystem.DoubleRiskActive flag in ApplyHPLoss)
- Added bargain-overlay to Encounter.uxml with flavor text, description, cost, accept button, timer bar
- Added encounter.uss bargain styles: amber-themed overlay panel, timer fill bar, accept button with hover/active states
- Modified EncounterController: subscribes to BargainOfferedEvent/BargainExpiredEvent, shows/hides bargain panel, accept button publishes BargainAcceptedEvent, timer fill updates via schedule.Execute().Every(250), hides on encounter start
- Updated GameSceneSetup: creates MoodBargainSystem GameObject

## Implementation Notes — #22
- Created RivalSystem.cs: singleton MonoBehaviour, subscribes to RunStartedEvent/EncounterStartedEvent/EncounterEndedEvent. On run start picks random non-boss/non-guide NPC archetype as rival (Riddlemaster, TricksterMerchant, SilentLibrarian), publishes RivalDesignatedEvent. Tracks rival encounter count as RivalTier, publishes RivalEncounterStartedEvent on encounter start and RivalDefeatedEvent on encounter win
- Added RivalDesignatedEvent, RivalEncounterStartedEvent, RivalDefeatedEvent to GameDataModels
- Modified PromptBuilder.BuildSystemPrompt(): injects rival tier context (dismissive at tier 1, grudging respect at 2, obsession at 3+)
- Modified MapController: caches rival-info Label, shows "RIVAL: {name}" in stats bar via UpdateStats()
- Modified Map.uxml: added rival-info Label in stats area
- Modified map.uss: added stat--rival style (danger color, red border)
- Modified EncounterController: subscribes to RivalEncounterStartedEvent, appends " [RIVAL]" to NPC name label, adds npc-name--rival CSS class
- Modified encounter.uss: added npc-name--rival style (color: danger)
- Modified RunManager: subscribes to RivalDefeatedEvent, awards bonus gold (5g per rival tier)
- Updated GameSceneSetup: creates RivalSystem GameObject

## Implementation Notes — #21
- Created UltimatumSystem.cs: subscribes to GuessSubmitted/EncounterStarted/ClueReceived/EncounterEnded events. Triggers when GuessesRemaining==1. Activates CRT clean mode, fires UltimatumTriggeredEvent, requests LLM ultimatum line, runs 15s countdown. Countdown expiry fires UltimatumExpiredEvent
- Added CRTSettings.SetCleanMode(bool): caches and zeroes scanlineIntensity, noiseIntensity, chromaticAberration, phosphorIntensity. Restores cached values when disabled
- Added LLMManager.GenerateUltimatumLineAsync(): uses PromptBuilder.BuildUltimatumPrompt for dramatic NPC line, falls back to generic line if LLM unavailable
- Added PromptBuilder.BuildUltimatumPrompt(): short high-drama prompt for NPC's final words, mood-aware, language-aware
- Added UltimatumTriggeredEvent, UltimatumExpiredEvent, UltimatumLineReceivedEvent to GameDataModels
- Added ultimatum-overlay to Encounter.uxml with ultimatum-text and countdown-text labels
- Added USS: ultimatum overlay with dim background transition, text fade-in, countdown blink at <5s
- Modified EncounterController: subscribes to 3 ultimatum events, manages countdown display via schedule.Execute().Every(250), hides on encounter start/end
- Modified EncounterManager: subscribes to UltimatumExpiredEvent, auto-fails encounter on expiry
- Updated GameSceneSetup: creates UltimatumSystem GameObject

## Implementation Notes — #20
- Created AdaptiveDifficultyMod.cs: subscribes to ClueReceivedEvent, maps mood→DifficultyShift (Mercy/Normal/Cruel), publishes DifficultyShiftChangedEvent. Mood mapping: frustrated/encouraging→Mercy, amused/taunting/menacing→Cruel, others→Normal
- Modified EncounterManager.RequestNextClue(): reads AdaptiveDifficultyMod.CurrentShift, Mercy adds +1 letter reveal, Cruel sets reveals to 0
- Modified PromptBuilder.BuildUserMessage(): accepts DifficultyShift param, injects "player is struggling, give clearer hint" (Mercy) or "player is doing well, be more oblique" (Cruel)
- Modified LLMManager.TryLLMClueAsync(): reads AdaptiveDifficultyMod and passes shift to PromptBuilder
- Added DifficultyShift enum + DifficultyShiftChangedEvent to GameDataModels.cs
- Added signal-status Label to Encounter.uxml (inside new clue-header row with clue-number)
- Added encounter.uss styles: signal-status with mercy (green) / cruel (red) CSS modifiers, fade transition
- Modified EncounterController: subscribes to DifficultyShiftChangedEvent, shows [SIGNAL: BOOSTED] (mercy/green) or [SIGNAL: DEGRADED] (cruel/red), resets on encounter start
- Updated GameSceneSetup: creates AdaptiveDifficultyMod GameObject in scene

## Implementation Notes — #48
- Deleted 18 legacy uGUI scripts: MainMenuUI, MapUI, ShopUI, RunEndUI, EncounterUI, TileBoardUI, GuessedLettersUI, NPCPortraitUI, UIAnimator, ButtonHoverEffect, BossEntranceUI, SuspenseEffects, TextSpinner, AnimatedCounter, TileGlowEffect, TerminalUIHelper, EncounterTestUI, LLMTestUI
- Migrated ScreenEffectsOverlay: RawImage → UI Toolkit VisualElement with runtime-generated textures (scanlines + vignette), now RequireComponent(UIDocument)
- Migrated AmbientDataStream: TextMeshProUGUI columns → UI Toolkit Label elements with flexbox row layout, now RequireComponent(UIDocument)
- GameSceneSetup rewritten: Removed Canvas, EventSystem, all uGUI factory methods (CreatePanel, CreateButton, CreatePrimaryButton, CreateInputField, CreateFullscreenImage, CreateContainer, CreateText, CreateStatChip, CreateEncounterStatChip, AddPanelAnimator, AddPanelBorder, SetAnchors), CreateEncounterPanel_Legacy, WireEncounterUI_Legacy. Added CreateAmbientDataStream() and CreateScreenEffectsOverlay() as UIDocument-based GameObjects with sort ordering (-100 for background, +100 for overlay). Reduced from 1361 → 433 lines
- DOTween removed entirely: Deleted Assets/Plugins/Demigiant/, Resources/DOTweenSettings.asset, cleaned DOTWEEN scripting define symbols from ProjectSettings.asset
- Zero remaining UnityEngine.UI imports in codebase (only UnityEngine.UIElements remains)
- TMPro still used by TerminalThemeSO (font asset references) and FontEvaluator (editor font utility) — valid dependencies

## Implementation Notes — #46
- Encounter.uxml: Full UXML layout — board frame with tile grid, category banner, NPC dialog card (portrait + name + clue), stats bar (HP with bar/gold/guesses/score chips), input area (prompt + TextField + submit/solve buttons), bottom panels (history + tomes split), guessed letters grid, result overlay, flash overlay
- encounter.uss: Comprehensive encounter styles — WoF-style tile cells with hidden/revealed/empty/flash/glow states, category banner with amber text, NPC dialog card, stat chips with color variants, input area with terminal prompt styling, bottom panels split layout, guessed letters with hit/miss colors, result overlay with visibility toggle, flash overlay with damage/success/boss CSS classes, boss color modifiers
- EncounterController.cs: UIDocument-based controller replacing uGUI EncounterUI — subscribes to 9 EventBus events (EncounterStarted, ClueReceived, GuessSubmitted, EncounterEnded, HPChanged, TomeTriggered, BossIntro, LetterRevealed, GameStateChanged), builds WoF tile board dynamically using same algorithm as TileBoardUI, builds A-Z guessed letters grid, typewriter effect via schedule.Execute().Every(), HP bar lerp via schedule.Execute().Every(16), flash overlay via CSS class toggle + delayed removal, tile reveal animation via flash→reveal→glow class sequence, NPC portrait expressions via NPCPortraitData with temporary revert scheduling, result overlay with staggered details typewriter + delayed continue button reveal, Enter key submit via KeyDownEvent
- GameSceneSetup: CreateEncounterPanel now creates UIDocument GO (like MainMenu/Map/Shop/Result), removed AddPanelAnimator for encounter panel, removed WireEncounterUI call, legacy methods preserved with _Legacy suffix for #48 cleanup
- DesignSystemTests: Added VerifyEncounterUIToolkit test (31 checks: 24 named elements, 3 Button types, 1 TextField type, 1 CSS class, 2 asset loads)
- Old EncounterUI.cs, TileBoardUI.cs, GuessedLettersUI.cs NOT deleted yet (will be removed in #48 "Remove Legacy uGUI Dependencies")

## Implementation Notes — #45
- Result.uxml: UXML layout with ASCII banner, victory/defeat title, 4 stat rows (score, encounters, tomes, words), separators, play-again button. Uses stagger-item classes for staged reveal
- result.uss: Full result screen styles — win/loss color modifiers (amber victory, red defeat), stat row layout with dotted bottom borders, separator styling, responsive content centering
- ResultController.cs: UIDocument-based controller replacing uGUI RunEndUI — subscribes to RunEndedEvent + GameStateChangedEvent, applies win/loss CSS modifier class, populates stats from RunManager/TomeManager, staggered entrance animation via schedule.Execute() with configurable delays
- GameSceneSetup: CreateRunEndPanel now creates UIDocument GO (like MainMenu/Map/Shop), removed AddPanelAnimator for run end panel
- DesignSystemTests: Added VerifyResultUIToolkit test (13 checks: 8 named elements, 1 Button type, 1 CSS class, 4 stat rows count, 2 asset loads)
- Old RunEndUI.cs NOT deleted yet (will be removed in #48 "Remove Legacy uGUI Dependencies")

## Implementation Notes — #44
- Shop.uxml: UXML layout with header (title + gold/HP stats), buy/sell ScrollView containers, feedback label, heal button with dynamic cost text, leave button
- shop.uss: Card styles with rarity-colored left stripes, hover transitions (bg + scale), sold state dimming, section headers (amber buy, magenta sell), heal button states, staggered entrance animations via opacity+translate
- ShopController.cs: UIDocument-based controller — dynamic card creation from ShopManager.Inventory, ClickEvent handlers for buy/sell/heal, rarity CSS classes for stripes and names, EventBus GameStateChanged subscription, staggered entrance via schedule.Execute()
- GameSceneSetup: CreateShopPanel now creates UIDocument GO (like MainMenu/Map), removed AddPanelAnimator for shop, wires ShopController → ShopManager
- DesignSystemTests: Added VerifyShopUIToolkit test (15 checks: 8 named elements, 2 Button types, 2 ScrollView types, 1 CSS class, 2 asset loads)
- Old ShopUI.cs NOT deleted yet (will be removed in #48 "Remove Legacy uGUI Dependencies")

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
