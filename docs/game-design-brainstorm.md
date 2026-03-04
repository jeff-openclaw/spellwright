# Spellwright Game Design Brainstorm

> **Status:** Iteration complete (20 iterations across 4 design pillars)
> **Date:** 2026-03-04
> **Process:** 4 parallel Opus agents, each doing 5 refinement iterations
> **Goal:** Make the game fun, surface the AI, add strategic depth

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Pillar 1: Making the AI Visible](#pillar-1-making-the-ai-visible)
3. [Pillar 2: New Powers & Mechanics](#pillar-2-new-powers--mechanics)
4. [Pillar 3: Encounter Variety](#pillar-3-encounter-variety)
5. [Pillar 4: Roguelike Meta-Depth](#pillar-4-roguelike-meta-depth)
6. [Cross-Pillar Synergies](#cross-pillar-synergies)
7. [Master Implementation Roadmap](#master-implementation-roadmap)
8. [Build Archetypes](#build-archetypes)

---

## Executive Summary

The core problem: Spellwright has an LLM generating clues but the player has no idea. Every encounter plays identically. There's no reason to do a second run. The game needs:

1. **AI Visibility** — Let players see and feel the AI working (mood-driven CRT, typing cadence, generation theater)
2. **Player Agency** — Meaningful decisions beyond "guess the word" (confidence bets, active tomes, wagers)
3. **Encounter Variety** — No two encounters should feel the same (modifiers, special types, speed rounds)
4. **Meta-Depth** — Permanent progression, unlockables, builds (Lexicon, Spellbooks, branching map, ascension)

**The unique identity:** Your vocabulary IS your power. The AI remembers you. The CRT terminal is alive.

---

## Pillar 1: Making the AI Visible

### MUST-HAVE

#### 1.1 Atmosphere Engine (Mood-Driven CRT)
The `mood` field from every LLM response (already parsed, currently unused) drives real-time CRT shader changes.

| Mood | Tint | Scanlines | Bloom | Stability | Feel |
|------|------|-----------|-------|-----------|------|
| `amused` | Warm amber | Low | Medium | Stable | Cozy, inviting |
| `cryptic` | Cool blue | High | Low | Slight drift | Mysterious |
| `taunting` | Red-orange | Medium | High | Flicker | Confrontational |
| `encouraging` | Soft green | Low | Medium | Rock steady | Safe |
| `menacing` | Near-black, purple | Max | None | Unstable | Dread |

**Implementation:** Add `CRTMoodPreset` + `LerpToMood()` to existing `CRTSettings` singleton. ~100 lines. Call from `EncounterManager` on clue response.

#### 1.2 Living Dialogue (Typewriter + Clue Memory)
Clues type out character by character with NPC-specific personality:

| NPC | Speed | Style |
|-----|-------|-------|
| Guide | 35ms/char | Calm, steady. Pauses before punctuation. |
| Riddlemaster | 60ms/char | Dramatic. Full stop = 400ms pause. |
| Librarian | 30ms/char | Fast, clinical, no flourish. |
| Trickster | 25ms/char | Erratic bursts. Fake-out backspaces ("the answer is—" → "wouldn't you like to know?") |
| Whisperer | 100ms/char | Agonizingly slow. Letters sometimes appear out of order. |

Previous clues shown as fading ghost text when clue #2+ arrives. ~180 lines.

#### 1.3 Terminal Generation Theater
While LLM generates, show themed "terminal commands" per NPC:

- **Guide:** `> CONSULTING THE ANCIENT LEXICON...`, `> SELECTING DIFFICULTY: GENTLE`
- **Riddlemaster:** `> ENCRYPTING MEANING...`, `> LAYERING MISDIRECTION [2/3]...`
- **Librarian:** `> QUERYING ARCHIVE INDEX...`, `> REFERENCE FOUND: VOL.XII, CH.4`
- **Trickster:** `> SHUFFLING DECEPTIONS...`, `> DISCARDING [TOO HONEST]...`
- **Whisperer:** `> ░░░░░░░░░░░░`, `> SIGNAL ACQUIRED...`, `> ORIGIN: [REDACTED]`

Final line resolves to `> MOOD: [actual mood]` from the response. Transforms wait time into spectacle. ~120 lines.

### NICE-TO-HAVE

#### 1.4 Crystallization Effect
During generation, word fragments from the active word pool flicker rapidly, decelerating, then "collapse" into the actual clue with a CRT glitch frame. Fragments are thematically related (same category) — attentive players get a meta-hint.

Boss variant: Whisperer fragments are from WRONG categories (deliberate obfuscation).

#### 1.5 The Aside (AI Commentary)
After the clue, a smaller italicized NPC comment on the player's behavior:
- "Four wrong guesses and you haven't tried a vowel. Bold."
- "Your gold hoard grows. The Trickster will be pleased."
- "Three encounters and you haven't lost a guess. The Whisperer will enjoy breaking that streak."

Two-tier: LLM-generated `aside` field (with template fallback for reliability). Rare fourth-wall breaks (10% chance): "I considered lying. The ancient rules forbid it."

### STRETCH

#### 1.6 Whisperer Signal Decay
Boss encounter progressively degrades the CRT: scanlines widen, signal drops (black frames), NPC name corrupts (`The Whisperer` → `T̸h̷e̶ W̸h̷i̵s̸p̵e̷r̶e̵r̸` → `░░░ ░░░░░░░░░░`), text jitters. Correct letters briefly restore signal. Solving = full "signal recovered" sequence. ~250 lines.

---

## Pillar 2: New Powers & Mechanics

### New Tomes

#### 2.1 Ink Well (Common, Insight)
Each clue received reveals +1 additional random tile (stacks with base `lettersRevealedPerClue`). Simple, always useful, rewards patience.

#### 2.2 Gold Tongue (Uncommon, Economy)
Solve on guess #1 or #2 → +5 bonus gold. Rewards risk-taking and speed.

#### 2.3 Burning Page (Uncommon, Offense) — ACTIVE TOME
**First active ability.** Player clicks a button during encounter: costs 5 HP, reveals 3 random tiles. The player chooses WHEN — creating a timing decision. Only available with 6+ hidden tiles.

**Architecture:** New `IActiveTomeEffect` interface with `UseActiveAbility()`. Foundation for all future active tomes.

### New Systems

#### 2.4 Encounter Mutators
Each non-tutorial encounter rolls for 0-1 mutators:

| Mutator | Effect | Reward |
|---------|--------|--------|
| **SHROUDED** | Category hidden ("???"), NPC told not to reference it | +50% gold/score |
| **CURSED TILES** | 2-3 tiles show wrong letters (see Pillar 3) | +25% gold |
| **DOUBLE OR NOTHING** | Wager gold before encounter | 2x reward on win |

Displayed in encounter UI header. Stack 2 mutators in later waves.

#### 2.5 Confidence Bet
Before each encounter (after seeing category + word length), optionally bet on solving in N guesses:
- 1 guess: 5x score
- 2 guesses: 3x score
- 3 guesses: 2x score
- Fail to meet target: 0 score (keep gold)

Pure skill bet. Good players extract massive scores. New players safely ignore.

#### 2.6 Streak System
Consecutive encounter wins build a streak (max 5):
- Streak 1: +1 gold per encounter
- Streak 2: +1 clue tile reveal
- Streak 3: +25% score
- Streak 4: +2 gold per encounter
- Streak 5: Free random Common tome

Loss resets to 0. Creates tension — a 4-streak makes the next encounter feel HIGH STAKES.

#### 2.7 Rest Node (Already in enum!)
`NodeType.Rest` exists but is unimplemented. Choose ONE:
- **Heal:** Restore 10 HP (free)
- **Study:** Preview next encounter's category + first letter
- **Upgrade:** Upgrade an equipped tome's rarity tier (increases effect magnitude)

Fixed position: node 3 of each wave (mid-wave breather).

#### 2.8 Tome Resonance (Set Bonuses)
Tomes have category tags (Insight, Defense, Economy). Holding 2+ of the same category:
- 2x Insight: Clues include +1 bonus tile
- 2x Defense: +2 HP shield at encounter start
- 2x Economy: Shop prices -15%
- 3x any: Bonus doubles

### Tome Expansion Target
Expand from 5 → 20+ tomes across 3 rarities:
- **Common (8):** Small bonuses (+1 guess, +2g per encounter, Ink Well, etc.)
- **Rare (7):** Strong effects (wrong guesses reveal a letter, first clue is 2 sentences, shops have +1 item)
- **Legendary (5):** Build-defining (all words from a chosen category, guessing in ≤2 heals 3 HP, letters guessed correctly in past encounters start revealed)

Rare/Legendary locked behind Lexicon milestones (see Pillar 4).

---

## Pillar 3: Encounter Variety

### Encounter Modifiers (Curses)

Applied randomly to encounters starting wave 2. Displayed in header.

| Curse | Effect | Strategy Shift |
|-------|--------|----------------|
| **NO VOWELS** | Vowel letter guesses always miss. Vowels only from reveals/tomes. | Consonant-first strategy |
| **MIRROR** | Board displayed in reverse letter order | Mental spatial flip |
| **FRAGILE** | HP costs doubled. NPC gives slightly better clues. | Conservative play |
| **STATIC** | No consolation reveals, no clue-bonus reveals | Pure deduction |
| **ECHO** | Each clue from a different random NPC personality | Shifting voice, disorienting |
| **GENEROUS** | HP costs halved, gold reward is 0 | "Rest" modifier |

### Special Encounter Types

#### 3.1 Speed Round
3 short words (3-5 letters), 10 seconds each, phrase-only, no HP cost. One clue per word. Solving all 3 = big bonus. Complete tempo change — adrenaline break from strategic play.

**CRT treatment:** Countdown timer as `> TIME: 00:07` in blinking text. Between words: `> NEXT SIGNAL INCOMING...` with loading bar. `> DECODED` / `> SIGNAL LOST` stamps.

#### 3.2 Anagram Mode
All letters shown scrambled. No clues. Fewer guesses (3-4). Phrase-only. Category is the sole hint. Wrong guess re-scrambles letters into a new arrangement for a fresh look.

**CRT treatment:** Letters "fall" into random positions with teletype animation. Header: `> SCRAMBLED TRANSMISSION // DECODE SEQUENCE`.

#### 3.3 Chain Encounter
Two linked words. Solve word 1 normally (3 guesses). Word 1 becomes the clue for word 2 (3 guesses + 1 NPC hint). Curated word pairs (RAIN→UMBRELLA, NEEDLE→HAYSTACK). 2x rewards.

**CRT treatment:** Terminal "reboots" between phases. `> LINKED SIGNAL:` header shows solved word.

#### 3.4 NPC Duel
Two NPCs give clues for the same word — but one is "possessed" and gives clues for a related-but-wrong word. Player can spend 1 guess to accuse. Correct accusation = false NPC silenced + bonus gold.

**CRT treatment:** Split-screen terminal windows. Clues type in parallel. Accused liar's window goes to static.

#### 3.5 Corrupted Tiles
2-4 tiles start showing wrong letters (flickering between incorrect chars). Correct letter guesses cleanse corruption. Player can't trust what they see.

**CRT treatment:** Corrupted tiles flicker with phosphor burn-in. Cleansing triggers "degauss" animation.

#### 3.6 Phantom Letters
Board fully "revealed" but contains 2-3 extra fake letters. Player identifies and "exorcises" phantoms by clicking them. Wrong exorcism = HP cost. Inverts the normal gameplay — carve away noise instead of building up.

**CRT treatment:** Phantoms have barely perceptible flicker (1-pixel offset). Exorcism = glitch-out vanish. Header: `> SIGNAL CONTAMINATION: IDENTIFY AND PURGE FALSE GLYPHS`.

#### 3.7 Gambit/Wager (Pre-Encounter Event)
Before certain encounters, wager gold. Solve in ≤3 guesses = 3x return. Fail = lose wager. 4-6 guesses = break even. Adds risk-reward decision BEFORE the encounter.

**CRT treatment:** "TRANSACTION TERMINAL" with ASCII coin art. `> WAGER LOCKED. NO REFUNDS.`

### Pacing Rules
1. Never two special encounters in a row
2. Never repeat same variant within a wave
3. Curses stack onto any type starting wave 2
4. Speed rounds can't have curses (fun break)
5. Boss always gets a unique modifier from wave 2+

### Variety Math
Before: 1 encounter structure.
After: 6 structures × 7 modifiers × gambit/no-gambit = **70+ unique configurations.**

---

## Pillar 4: Roguelike Meta-Depth

### TIER 1: Essential

#### 4.1 The Lexicon (Permanent Word Collection)
Every successfully guessed word is saved permanently. Main menu Lexicon screen shows all discovered words by category. This is the foundation for ALL unlocks.

**Milestones:**
| Words | Unlock |
|-------|--------|
| 10 | First alternate Spellbook |
| 25 | Cursed Run modifier slot |
| 50 | Rare tome pool |
| 100 | Legendary tome pool |
| 200 | Mixed-language word pools |
| Per-category complete | Category-specific tome |

Words track: first guess date, best attempt, times encountered. "New Word" indicator during encounters.

#### 4.2 Branching Map
Replace linear corridor with a Slay the Spire-style branching node map:
- 3-4 rows of 2-3 nodes each, converging at boss
- Node types: Encounter, Elite, Event, Rest, Shop
- Player sees full map and chooses path
- Each wave generates new map segment
- Visual: ASCII-art map with connecting lines on CRT

#### 4.3 Ascension System
Beat the boss → unlock Ascension 1. 10 cumulative levels:

| Level | Modifier |
|-------|----------|
| A1 | -1 max guesses per encounter |
| A2 | Shops cost 20% more |
| A3 | Elite encounters mandatory on at least one path |
| A4 | -5 starting HP |
| A5 | Boss has two phases (two words) |
| A6 | Rest sites only heal 50% |
| A7 | One random letter reveal is a decoy (wrong letter) |
| A8 | Clues have 1-sentence maximum |
| A9 | No consolation reveal on wrong letter |
| A10 | All modifiers + hard-only word pools |

### TIER 2: High Impact

#### 4.4 Spellbooks (Character Classes)
Different starting conditions + unique passive. Unlocked via Lexicon milestones.

| Spellbook | HP | Gold | Passive |
|-----------|-----|------|---------|
| **The Scholar** | 35 | 0 | First clue reveals 2 letters instead of 1 |
| **The Gambler** | 20 | 20 | Wrong phrase guesses have 30% chance to not cost HP |
| **The Polyglot** | 25 | 5 | Mixed-language words. Bonus gold for Romanian words. |
| **The Archivist** | 30 | 5 | Lexicon words re-encountered start with 2 letters revealed |
| **The Speedster** | 25 | 5 | Timer on each encounter (60s). Bonus gold for fast solves. |

Start with Scholar only. Others unlock at Lexicon 10/25/50/100.

#### 4.5 NPC Relationship Progression
Persistent `relationshipLevel` (0-5) per NPC across runs. Level up on successful encounters, down on failure.

| Level | Effect |
|-------|--------|
| 0 | Suspicious/terse, vague clues |
| 1 | Neutral, standard clues |
| 2 | Friendly, occasionally gives bonus hint |
| 3 | Reveals lore, better clue quality |
| 4 | Offers a unique tome (only available at this level) |
| 5 | Easier encounter (bonus starting letters) + story ending |

**The AI twist:** The LLM system prompt literally changes per relationship level. The Guide at level 0: "I don't know if I can trust you..." At level 5: "Ah, my old friend! Let me give you a generous hint..."

#### 4.6 Tome Expansion + Synergies
See Pillar 2. 5 → 20+ tomes, 3 rarities, category set bonuses. Rare/Legendary gated behind Lexicon milestones.

### TIER 3: Polish & Endgame

#### 4.7 Event Nodes (Special Word Puzzles)
Non-encounter nodes on the branching map:
- **Anagram Challenge** — Scrambled word, no clues
- **Riddle Gate** — LLM poses a riddle, single guess, great reward or nothing
- **Letter Auction** — Bid gold to see letters one at a time, then guess
- **Merchant's Bargain** — Pay gold for letter reveals before guessing

#### 4.8 Daily Challenge Runs
Seeded run: fixed word pools, map, shops, NPCs. Global leaderboard. One attempt per day. The competitive retention hook.

### TIER 4: Future

- **Cursed Run Modifiers** — "Clues are metaphors only," "one-word clues" (changes AI prompt)
- **Lore Fragments** — NPC-driven narrative across runs, synergizes with relationships
- **Word Mastery** — Per-word performance tracking, "mastered" words give bonuses

---

## Cross-Pillar Synergies

### The AI Is Everywhere
- **Mood CRT** (P1) + **Curses** (P3): ECHO curse changes NPC voice AND CRT mood shifts between clues
- **Generation Theater** (P1) + **NPC Duel** (P3): Two parallel generation theaters — one per NPC
- **Asides** (P1) + **Streaks** (P2): "Three encounters without a miss. The Whisperer can smell that."
- **Typing Cadence** (P1) + **Speed Round** (P3): Speed round types at 10ms/char — frantic urgency
- **Signal Decay** (P1) + **Ascension** (P4): Higher ascension = Whisperer decay starts faster

### The Lexicon Powers Everything
- **Lexicon** (P4) gates **tome rarities** (P2) and **Spellbooks** (P4)
- **Archivist Spellbook** (P4) rewards **known words** from the Lexicon
- **Word Mastery** (P4) adds personal progression to every encounter

### Builds Create Replayability
- **Scholar build:** Ink Well + First Light + Vowel Lens + Study at rest sites → information maximizer
- **Gambler build:** Gold Tongue + Second Wind + Confidence Bets + Wagers → risk/reward maximizer
- **Tank build:** Thick Skin + Burning Page + Second Wind + Heal at rest → attrition/survival
- **Echo Mage:** Echo Chamber + Vowel Lens + Second Wind → deliberate wrong guesses for reveals

---

## Master Implementation Roadmap

### Phase 1: Foundation (Estimated: 1-2 weeks)
**Goal:** Make the AI visible and encounters varied.

1. Atmosphere Engine (Mood CRT) — ~100 lines, uses existing CRTSettings
2. Terminal Generation Theater — ~120 lines, new component
3. Living Dialogue (Typewriter) — ~180 lines, new component
4. Shrouded Mutator — ~60 lines across existing files
5. Gambit/Wager — ~80 lines + small UI panel

### Phase 2: Player Agency (Estimated: 1-2 weeks)
**Goal:** Give players meaningful decisions.

6. Ink Well + Gold Tongue tomes — ~60 lines each, follows existing pattern
7. Rest Node implementation — ~150 lines, enum already exists
8. Confidence Bet system — ~100 lines + UI buttons
9. Streak System — ~80 lines in RunManager + UI counter
10. Burning Page (active tome architecture) — ~200 lines, new interface

### Phase 3: Encounter Variety (Estimated: 2-3 weeks)
**Goal:** No two encounters feel the same.

11. Cursed Encounters (6 modifiers) — ~200 lines across existing systems
12. Speed Round — ~250 lines, new sub-state
13. Anagram Mode — ~150 lines, display logic
14. Corrupted Tiles — ~180 lines, tile state extension
15. Crystallization Effect — ~120 lines, extends generation theater

### Phase 4: Meta-Depth (Estimated: 3-4 weeks)
**Goal:** "One more run" hooks.

16. The Lexicon — ~300 lines + persistent storage + UI screen
17. Branching Map — ~500 lines, significant MapUI rewrite
18. Ascension System — ~150 lines, modifier flags
19. Spellbooks (3-4 classes) — ~200 lines + SO assets + selection UI
20. NPC Relationship Progression — ~250 lines + persistent storage + prompt changes

### Phase 5: Polish & Endgame (Estimated: 2-3 weeks)
21. Tome Expansion (15 more tomes) + Synergies
22. Chain Encounter + NPC Duel
23. Phantom Letters
24. Aside system (AI commentary)
25. Whisperer Signal Decay (boss atmosphere)
26. Daily Challenge Runs

---

## Build Archetypes

These emerge naturally from the new systems and should be the design north star:

### "The Scholar" (Information Maximizer)
- **Spellbook:** Scholar (+2 letter reveals on clue 1)
- **Tomes:** Ink Well + First Light + Vowel Lens
- **Strategy:** Wait for clues, let tomes reveal the board. Solve on guess 3-4 with near-complete board.
- **Rest choice:** Study (preview next word's category)
- **Weakness:** Slow. Low score multiplier. Doesn't benefit from Confidence Bet.

### "The Gambler" (Risk/Reward Maximizer)
- **Spellbook:** Gambler (30% dodge on wrong phrase HP)
- **Tomes:** Gold Tongue + Second Wind
- **Strategy:** Always bet. Always wager. Go for guess #1 solves.
- **Preferred:** Double or Nothing encounters, Confidence Bets
- **Weakness:** Volatile. Bad words end the run fast.

### "The Tank" (Attrition/Survival)
- **Spellbook:** Scholar or Archivist
- **Tomes:** Thick Skin + Burning Page + Second Wind
- **Strategy:** Trade HP for information. Grind through encounters. Never die.
- **Rest choice:** Heal (maintain HP pool)
- **Weakness:** Low gold, low score. Survives but doesn't climb leaderboards.

### "The Echo Mage" (Wrong-Guess Synergy)
- **Spellbook:** Any
- **Tomes:** Echo Chamber + Vowel Lens + Second Wind
- **Strategy:** Deliberately guess common words that share letters. Echo reveals shared letters, Vowel Lens reveals vowels. Two wrong guesses = board nearly solved.
- **Weakness:** Burns guesses fast. Must solve by guess 3-4.

---

## The Player Journey

### Hour 0-1 (Run 1-2): "This is a cool word game"
- Scholar only, linear map, common tomes
- CRT mood shifts and generation theater create atmosphere
- Lexicon notification after run 1: "12 words collected!"

### Hour 1-3 (Run 3-6): "There's more here than I thought"
- First modifiers appear (Shrouded, first curse)
- Lexicon unlocks Cursed Run slot and new Spellbook
- NPCs hit relationship level 2 — noticeably friendlier
- First event node (Anagram Challenge)

### Hour 3-6 (Run 7-12): "Each run feels different"
- Rare tomes unlock. First synergy discovered.
- Gambler Spellbook unlocked — completely different playstyle
- Ascension 1 unlocked. Streaks create real tension.
- Elite encounters on map. Risk vs reward.

### Hour 6-15 (Run 13-30): "I'm building something"
- Legendary tomes. Game-changing builds.
- Polyglot Spellbook — Romanian words mixed in.
- Ascension 3-5. Specific builds become necessary.
- NPCs at level 4-5. Lore assembling. Unique tome offers.

### Hour 15-50+ (Run 30+): "I know this game deeply"
- Ascension 6-10. Brutal. Perfect synergies required.
- Lexicon nearing category completion.
- Daily challenge competition.
- "One more run" is not a question — it's a reflex.

---

## Resume Notes

To continue this brainstorming process:
1. All 4 pillar agents produced 5 iterations each (20 total iterations)
2. Raw agent outputs are in `/tmp/claude-*/tasks/` (ephemeral)
3. This document captures the refined, cross-referenced output
4. Next step: Pick a phase and start implementing
5. Recommended start: Phase 1 (Mood CRT + Generation Theater + Typewriter) — highest visual impact, lowest risk, uses existing systems

---

# Generator/Critic Brainstorm Passes (AI Visibility Focus)

> **Checklist for every idea (must pass ALL 4):**
> - [ ] Player sees/hears feedback within 5 seconds of triggering it
> - [ ] It uses the word/spelling mechanic as input
> - [ ] It changes based on NPC AI state (makes the AI *visible*)
> - [ ] Could be implemented in Unity in under 2 days

---

## Pass 1 | 2026-03-04 14:00 | Generator
### New ideas

- **Mood Pulse Tiles**: When a ClueResponse arrives, each tile on the board briefly pulses with a color mapped to the `mood` field (amber for amused, blue for cryptic, red for taunting, green for encouraging, purple for menacing). The pulse radiates outward from the center tile with a staggered delay (50ms per tile), so the player literally watches the NPC's emotional state wash across the board every time a clue lands. Correct letter guesses cause a secondary "confirmation" pulse in white; wrong guesses trigger a sharp red flash. Implementation: subscribe to `ClueReceivedEvent`, read `Clue.Mood`, map to a Color via a `MoodColorMap` dictionary, then fire a coroutine on TileBoardUI that lerps each tile's background color from moodColor back to default over 0.6s with position-based delay. ~80 lines in TileBoardUI + 20 lines for MoodColorMap. | Checklist: ✅✅✅✅

- **NPC Patience Meter**: A visible bar or counter in the encounter UI that represents the NPC's patience level. It starts full. Each wrong guess drains it by a chunk. Each correct letter restores a sliver. When patience drops below 50%, the NPC's next clue gets the `mood` overridden to "frustrated" via an extra field injected into the PromptBuilder user message (e.g., "You are getting frustrated with the player's wrong guesses"). When patience hits 0%, the NPC gives one final cryptic-angry clue and then the CRT scanlines spike to maximum for the remainder of the encounter. The player can SEE this bar ticking down and FEEL the NPC reacting to their play. Implementation: new `NPCPatienceTracker` component (~60 lines), hooks into `GuessSubmittedEvent` to update, publishes a `PatienceLevelChangedEvent` for UI. PromptBuilder adds a frustration clause when patience < 50%. UI is a simple filled bar next to the NPC name. CRTSettings gets a `SetScanlineOverride(float)` method. ~120 lines total. | Checklist: ✅✅✅✅

- **Clue Echo Trail**: After clue #2+ arrives, previous clues don't just disappear — they remain visible as ghost text with decreasing opacity (100% → 60% → 30%). But here's the AI-visible twist: each ghost clue's text color is tinted by its original `mood` (the mood it was generated with). As the encounter progresses, the player sees a visible trail of the NPC's emotional arc — maybe calm-blue, then amused-amber, then frustrated-red. If they're doing poorly, the trail turns angry. If they're doing well, it stays warm. Implementation: `ClueHistoryPanel` component on EncounterUI. Stores `List<(string clue, string mood)>`. On new `ClueReceivedEvent`, pushes the old clue to the trail and instantiates a TextMeshPro with alpha and mood-color tint. Max 3 visible trail entries. ~90 lines. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Clue Echo Trail

### Killed this pass
- (none — first pass)

### Resume token
LAST_PASS=1 | BEST_IDEAS=3 | NEXT_ROLE=Critic

---

## Pass 2 | 2026-03-04 14:15 | Critic
### Analysis

The weakest idea is **Clue Echo Trail**. Here's why it fails in practice:

1. **Passive, not reactive.** The player doesn't DO anything to trigger it — it just accumulates as a side effect of clues arriving. The mood-tinted colors are subtle enough that most players won't notice the emotional arc across 3 small ghost-text entries, especially when they're focused on the tile board and their next guess.
2. **Redundant with Mood Pulse Tiles.** If tiles already pulse with mood color on every clue, showing mood color on ghost text too is doubling up on the same signal. The tile pulse is more visceral because it happens ON the thing the player is staring at.
3. **Low information density.** Three fading text strings with slightly different tints doesn't communicate "the AI is alive" — it communicates "here's a chat log with colored text." It's decoration, not interaction.

**Replacement — Reactive Clue Intensity (NPC Tells):** After each guess, the NPC's next clue changes visually based on how the guess went — and the player can SEE this before reading the clue text. Specifically: when the LLM returns `mood`, the clue text's typewriter speed, font size, and CRT noise level all shift to match. An "amused" mood types fast with low noise. A "frustrated" mood types slowly with high noise and slightly larger text (the NPC is SHOUTING). A "cryptic" mood types at medium speed but each character has a random 0-200ms jitter (feels uncertain, deliberate). The player learns to READ the NPC's body language through how the text physically appears. This is deeply tied to AI state — the mood comes from the LLM, and it changes based on the player's guesses (which the prompt includes). Implementation: `ClueTypewriterEffect` component. On `ClueReceivedEvent`, reads `Clue.Mood`, maps to a `TypewriterProfile` (speed, jitter, fontSize scale, CRT noise override). Calls `CRTSettings.Instance.noiseIntensity = profile.noise` during typing, restores after. Uses DOTween for character-by-character reveal. ~130 lines. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)

### Killed this pass
- Clue Echo Trail — passive decoration, redundant mood signal with Pulse Tiles, no player interaction trigger

### Resume token
LAST_PASS=2 | BEST_IDEAS=3 | NEXT_ROLE=Generator

---

## Pass 3 | 2026-03-04 14:30 | Generator
### New ideas

- **Vowel Scarcity Radar**: When the player guesses a letter, the NPC AI "reacts" to HOW CLOSE the guess was to letters in the word. A new field is added to the PromptBuilder user message: whether the player's most recent guess was a vowel or consonant, and whether it hit or missed. The NPC's mood in the LLM response naturally shifts based on this context. On the UI side, a small CRT-styled "signal strength" indicator (3 bars, like a wifi icon but pixelated) appears next to the guess input. It reads the board state: if the ratio of hidden vowels to hidden consonants is high (lots of vowels still hidden), the bars glow warm — hinting that vowel guesses would be productive. If consonants dominate the hidden tiles, bars go cool. This is a BOARD-STATE signal that the AI implicitly knows about (since the AI has the word). Implementation: `SignalRadar` component subscribes to `LetterRevealedEvent` and `GuessSubmittedEvent`. Reads `BoardState.Tiles` to compute hidden vowel ratio. Renders 3 bars as UI Images with color lerp. Prompt change: ~10 lines in PromptBuilder to append guess-type context. ~70 lines for radar UI. | Checklist: ✅✅✅✅

- **NPC Interrupt (Taunt on Repeat)**: If the player guesses a letter they've already tried (which currently just shows "You already guessed that letter!"), the NPC delivers a short, personality-specific taunt INSTEAD of the generic feedback string. The taunt comes from a small per-NPC string pool (5-8 lines each, no LLM call needed for speed) and is styled with the NPC's mood color and typewriter profile (from Reactive Clue Intensity). Guide says "I believe we covered that one, friend." Trickster says "Again? Delightful — I love watching you spin in circles." Whisperer says "...you forget so quickly..." The text appears in the clue area with a brief CRT glitch frame (1-frame chromatic aberration spike). This makes the NPC feel PRESENT even between clue generations, reacting to player mistakes in real time. Implementation: `NPCTauntBank` ScriptableObject with per-archetype string arrays. ~15 lines in `EncounterUI` to intercept the `IsLetterAlreadyGuessed` path and display taunt instead of generic text. CRT glitch: single-frame `chromaticAberration` spike via coroutine. ~60 lines total. | Checklist: ✅✅✅✅

- **Board Heartbeat (AI Breathing)**: While the LLM is generating a clue (the async wait period), the tile board subtly "breathes" — tiles scale up/down by 2-3% in a sine wave pattern at a speed that corresponds to the NPC's personality. Guide: slow, steady pulse (1.5s period). Riddlemaster: medium, slightly irregular. Trickster: fast, erratic (random per-tile phase offsets). Whisperer: extremely slow, with occasional sharp spikes (like a cardiac arrhythmia). When the clue arrives (`ClueReceivedEvent`), the breathing stops and tiles snap to their normal scale with a micro-bounce. The player SEES the board "thinking" while the AI generates, and the character of the thinking matches the NPC. Implementation: `BoardBreathingEffect` component. Starts on generation request (new `ClueGeneratingEvent` published by EncounterManager before the LLM call). Stops on `ClueReceivedEvent`. Per-NPC `BreathingProfile` with period, amplitude, and phase jitter. Uses DOTween or manual sine in Update. ~100 lines. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- Vowel Scarcity Radar
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=3 | BEST_IDEAS=6 | NEXT_ROLE=Critic

---

## Pass 4 | 2026-03-04 14:45 | Critic
### Analysis

The weakest idea is **Vowel Scarcity Radar**. Here's why:

1. **It doesn't actually use the AI.** The signal strength indicator reads board state (hidden vowel/consonant ratio), which is pure local computation. The prompt addition ("the player guessed a vowel") is cosmetic — the AI already receives the full guess list. The radar's behavior would be identical whether or not the AI existed. This violates the spirit of checklist item 3: "changes based on NPC AI state." The radar changes based on BOARD state, not AI state.
2. **It's a strategy hint that undermines the word-guessing tension.** Telling the player "try vowels now" flattens the decision space. Part of the fun of Wheel of Fortune is the uncertainty of whether to buy a vowel or guess a consonant. A signal that telegraphs the optimal move removes that friction.
3. **Visually noisy for low payoff.** A 3-bar wifi icon adds clutter to an already dense encounter UI (tile board, clue text, guessed letters, HP/gold, input field) without earning its screen real estate.

**Replacement — Mood Glitch Transition (CRT Mood Cuts):** When the NPC's mood CHANGES between consecutive clues (e.g., clue #1 was "amused," clue #2 is "frustrated"), the CRT performs a brief but dramatic "channel switch" glitch: 2-3 frames of static, a horizontal screen-tear, and then the new mood's visual treatment fades in (tint, scanline intensity, noise level — directly manipulating existing `CRTSettings` fields). If the mood stays the same between clues, there's no glitch — just a smooth continuation. This means the player can FEEL whether their guesses are changing the AI's emotional state. A mood shift from "encouraging" to "frustrated" (they guessed wrong) hits with a visible jolt. A stable mood means the AI is steady. Implementation: `CRTMoodTransition` component subscribes to `ClueReceivedEvent`. Stores `lastMood`. On mood change, triggers a coroutine: spike `noiseIntensity` to 0.12 for 3 frames, offset `chromaticAberration` to 0.008 for 2 frames (screen tear), then DOTween-lerp `scanlineIntensity`, `noiseIntensity`, `brightness`, and a new `screenTint` color to the target mood's preset over 0.5s. Mood presets stored in a `Dictionary<string, CRTMoodPreset>`. ~110 lines. Uses ALL existing CRTSettings fields — no shader changes needed. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)

### Killed this pass
- Vowel Scarcity Radar — not actually AI-driven (reads board state, not NPC state), undermines guessing tension, UI clutter for low payoff

### Resume token
LAST_PASS=4 | BEST_IDEAS=6 | NEXT_ROLE=Generator

---

## Pass 5 | 2026-03-04 15:00 | Generator
### New ideas

- **NPC Signature Sound on Guess (Mood-Keyed Audio Cue)**: Each NPC has a short, distinctive audio signature (a chord, chime, or synth tone — 0.3-0.5 seconds) that plays immediately when the player submits a guess. The PITCH of the sound shifts based on the NPC's current mood: "amused" plays the signature at normal pitch, "frustrated" drops it by a minor third (darker), "cryptic" adds a reverb tail (mysterious), "menacing" plays it reversed and distorted. Correct letter guesses play the tone ascending; wrong guesses play it descending. The player hears the NPC's personality and emotional state EVERY time they interact. Since the mood comes from the AI's last clue response, this makes the LLM's internal state audible. Implementation: `NPCAudioSignature` component with per-archetype AudioClip references. On `GuessSubmittedEvent`, plays the clip via `AudioSource.PlayOneShot()` with pitch set from a `MoodPitchMap`. Correct/wrong determined from `GuessResult.IsCorrect` or `IsLetterInPhrase`. 5 AudioClips (one per NPC) stored as assets. ~70 lines of code. | Checklist: ✅✅✅✅

- **AI Confidence Preview (Clue Certainty Flicker)**: When the LLM returns a clue, its `GenerationTimeMs` field (already tracked on `ClueResponse`) is used as a proxy for how "confident" the AI was. Fast generation (<2s) = the NPC was sure, and the clue text appears crisp and steady. Slow generation (>5s) = the NPC struggled, and the clue text has a subtle ongoing flicker (opacity oscillates between 85%-100% at 3Hz) that persists for the encounter until the next clue. Medium generation = slight shimmer. The player sees: "this clue came easy to the NPC" vs "the NPC is uncertain about this one." Harder words naturally take longer to generate, so this creates an organic difficulty signal baked into the AI's actual compute time. Implementation: `ClueConfidenceDisplay` component. On `ClueReceivedEvent`, reads `Clue.GenerationTimeMs`, maps to a `ConfidenceLevel` enum (Certain/Neutral/Uncertain), applies a DOTween loop on the clue TextMeshPro's alpha if uncertain. Stops the loop when the next clue arrives. ~50 lines. | Checklist: ✅✅✅✅

- **Tile Corruption on NPC Frustration**: When the NPC's mood is "frustrated" or "menacing" (from the LLM response), 1-2 revealed tiles on the board temporarily "corrupt" — their displayed letter scrambles through random characters for 1.5 seconds before settling back to the correct letter. This is purely cosmetic (the underlying BoardState is unchanged), but it makes the player FEEL the NPC's hostility bleeding into the game world. The corruption only targets REVEALED tiles (not hidden ones), so it messes with information the player already has, creating a moment of "wait, was that an 'E' or an 'A'?" panic. Different NPCs corrupt differently: Trickster swaps to visually similar letters (O→0, I→1, l→I). Whisperer replaces with Unicode glitch characters. Guide never corrupts (too friendly). Implementation: `TileCorruptionEffect` component subscribes to `ClueReceivedEvent`. If mood is frustrated/menacing, picks 1-2 random revealed tile GameObjects, runs a coroutine that swaps their `TextMeshPro.text` through 6-8 random chars over 1.5s, then restores the original. Per-NPC corruption char sets stored in a small dictionary. ~80 lines. No BoardState changes needed. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- NPC Signature Sound on Guess (Mood-Keyed Audio Cue)
- AI Confidence Preview (Clue Certainty Flicker)
- Tile Corruption on NPC Frustration

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=5 | BEST_IDEAS=9 | NEXT_ROLE=Critic

---

## Pass 6 | 2026-03-04 16:00 | Critic
### Analysis

The weakest idea is **AI Confidence Preview (Clue Certainty Flicker)**. Here's why it fails:

1. **Generation time is not AI state — it's infrastructure state.** The `GenerationTimeMs` value depends on hardware speed, Ollama model size, GPU load, prompt length, and system memory pressure. A player with a fast GPU will see every clue come in "confident" (no flicker). A player with a slow CPU will see every clue flicker "uncertain." The signal has nothing to do with how the NPC "feels" — it measures how fast the machine is. This fundamentally violates checklist item 3: "changes based on NPC AI state." It changes based on *hardware* state.
2. **Indistinguishable from existing CRT noise.** The game already has a CRT shader with scanlines, noise, and chromatic aberration. A subtle 85%-100% alpha oscillation on the clue text will be swallowed by the existing visual noise floor. Players will not register "the clue is flickering because the NPC was unsure" — they'll register "the CRT effect is doing its thing."
3. **Conflicts with Reactive Clue Intensity.** That idea already modifies how the clue text appears (typewriter speed, font size, CRT noise) based on mood. Adding a SECOND text-presentation modifier based on a DIFFERENT signal (generation time) creates visual confusion. Two systems fighting over how the clue text looks will produce muddy, unreadable results.

**Replacement — NPC Grudge Memory (Cross-Encounter Callback):** When a player enters a new encounter, if they performed poorly in the previous encounter with that same NPC archetype (lost 2+ guesses or took 3+ HP damage), the NPC's FIRST clue includes a memory reference injected into the PromptBuilder system prompt: "The player struggled with your last word. You remember this." The LLM naturally weaves this into its clue personality — the Trickster might open with mockery, the Guide with extra encouragement. On the UI side, when this "grudge" or "memory" context is active, the NPC's name in the encounter header displays with a small visual tell: a pulsing glow (warm for positive memory, cold for negative). The player sees the NPC nameplate glowing and KNOWS: "this NPC remembers my last encounter." The glow fades after clue #1 lands. Implementation: `NPCMemoryTracker` stores last encounter performance per NPC archetype in RunManager (just a `Dictionary<string, EncounterResult>` — no persistence needed, run-scoped only). PromptBuilder checks the tracker and appends a memory clause to the system prompt. `EncounterUI` reads the memory state on encounter start and applies a DOTween color pulse to the NPC name TextMeshPro. ~90 lines across 3 files (NPCMemoryTracker ~40, PromptBuilder +15, EncounterUI +35). | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- NPC Signature Sound on Guess (Mood-Keyed Audio Cue)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)

### Killed this pass
- AI Confidence Preview (Clue Certainty Flicker) — generation time measures hardware speed not AI state, indistinguishable from CRT noise, conflicts with Reactive Clue Intensity

### Resume token
LAST_PASS=6 | BEST_IDEAS=9 | NEXT_ROLE=Generator

---

## Pass 7 | 2026-03-04 16:15 | Generator
### New ideas

- **Letter Heat Map (NPC Suspicion Glow)**: When the player guesses a WRONG letter, the NPC's next clue response includes the `mood` field reflecting their reaction to the miss. On the UI side, the guessed-letters panel (which shows all tried letters) highlights the wrong letters with a glow color derived from the current mood. Guessing "X" wrong while the NPC is "amused" makes the X glow warm amber. Guessing "T" wrong while the NPC is "frustrated" makes it glow angry red. Over the course of an encounter, the guessed-letters panel becomes a heat map of the NPC's emotional journey — a trail of colored wrong letters telling a story. Correct letters always glow a consistent bright white. The player glances at the panel and reads the NPC's attitude shift THROUGH their own mistakes. Implementation: `GuessedLettersMoodTint` component on the existing `GuessedLettersUI`. On `ClueReceivedEvent`, stores current `mood`. On `LetterGuessedEvent`, if the letter missed, applies the stored mood's color to that letter's TextMeshPro via a `MoodColorMap` dictionary (reused from Mood Pulse Tiles). Correct hits get white. ~55 lines, purely additive to existing UI. | Checklist: ✅✅✅✅

- **NPC Stutter on Near-Miss (Phonetic Proximity Feedback)**: When the player guesses a letter that is NOT in the word but is phonetically or alphabetically adjacent to a letter that IS in the word (e.g., guessing "B" when "P" is present, or "N" when "M" is present), the NPC's clue text display briefly stutters — the typewriter effect hiccups, repeating the last 2 characters before continuing. This tells the player "you were CLOSE" without revealing what the correct letter is. The stutter is personality-flavored: Guide's stutter is gentle (tiny pause), Trickster's stutter is exaggerated (repeats 4 characters with a backspace animation), Whisperer's stutter makes the whole clue text jitter spatially. The proximity map is hardcoded (26 letter pairs) and checked locally — no LLM call needed for the proximity check, but the stutter displays differently based on the current NPC personality (which comes from the AI). Implementation: `NearMissDetector` with a static `Dictionary<char, char[]>` of adjacent letters. On `LetterGuessedEvent` where the letter missed, checks proximity against `BoardState.TargetPhrase`. If near-miss, publishes `NearMissEvent`. `ClueTypewriterEffect` (from Reactive Clue Intensity) subscribes and triggers stutter on the active clue text. Per-NPC stutter profiles (repeat count, jitter amplitude). ~90 lines across 2 components. | Checklist: ✅✅✅✅

- **Mood Shift Narration (Terminal Aside)**: When the NPC's mood changes between consecutive clues (detected same as Mood Glitch Transition), a one-line "terminal narration" appears below the clue in a different font style (monospace, dimmer, prefixed with `>`). These are short, NPC-specific narrative beats drawn from a curated pool per mood transition: "amused→frustrated" for Riddlemaster might show `> The Riddlemaster's smile fades.` "encouraging→cryptic" for Guide might show `> The Guide chooses her next words carefully.` "neutral→menacing" for Whisperer: `> The signal darkens.` These fire ONLY on mood transitions (not every clue), so they're rare and impactful — maybe 1-2 per encounter. The text fades out over 4 seconds. Implementation: `MoodTransitionNarrator` component. Stores `lastMood`. On `ClueReceivedEvent`, compares moods. If different, selects a line from `MoodTransitionBank` ScriptableObject (keyed by `[npcArchetype][fromMood→toMood]`, ~40 curated strings total). Instantiates a fading TextMeshPro below the clue area. ~75 lines + the SO data asset. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- NPC Signature Sound on Guess (Mood-Keyed Audio Cue)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=7 | BEST_IDEAS=12 | NEXT_ROLE=Critic

---

## Pass 8 | 2026-03-04 16:30 | Critic
### Analysis

The weakest idea is **NPC Signature Sound on Guess (Mood-Keyed Audio Cue)**. Here's why:

1. **The game has no audio system.** Spellwright currently has zero sound — no music, no SFX, no AudioSource setup. Adding "mood-keyed audio" requires first building an entire audio foundation: AudioMixer, AudioSource on a manager object, volume settings, mute toggle. The idea description casually says "5 AudioClips stored as assets" and "~70 lines" — but it omits the 150+ lines of audio infrastructure (AudioManager singleton, settings persistence, WebGL/platform considerations). This pushes it well past the 2-day implementation window when you factor in sourcing or generating 5 distinct NPC audio signatures, tuning pitch curves per mood, and making it not sound terrible. Checklist item 4 is borderline failed.
2. **Audio without context sounds random.** A 0.3-second synth tone on every guess submission — with pitch shifts the player hasn't been trained to interpret — will feel like noise, not feedback. Players need visual pairing to learn "lower pitch = frustrated." But if we're already doing Mood Pulse Tiles, Reactive Clue Intensity, and CRT Mood Cuts visually, the audio is redundant signal on the same channel.
3. **Mood doesn't change per-guess, it changes per-clue.** The mood comes from the LLM response, which arrives once per clue cycle (every 2-4 guesses). But this idea plays a sound on EVERY guess submission. Between clues, the mood is static — so the player hears the exact same pitched tone 3 times in a row. That's not "reactive," it's a repeating beep.

**Replacement — NPC Typing Hesitation (Live Generation Tells):** While the LLM is generating and the terminal generation theater is playing, the typing speed of the theater text visibly changes based on the NPC's LAST mood. If the NPC was "amused" in the previous clue, the generation theater types smoothly and quickly — the NPC is enjoying this. If the mood was "frustrated," the theater text types in halting bursts with long pauses, as if the NPC is struggling to formulate a clue. If "cryptic," the theater text types backward for brief stretches before correcting itself. If "menacing," the theater text accelerates aggressively, each line faster than the last. This transforms the already-planned generation theater from a static text dump into a live emotional readout. The player watches the generation happen and can FEEL whether the NPC is confident, struggling, or hostile BEFORE the clue even lands. Implementation: `GenerationTheaterTyper` component (extends the existing terminal generation theater concept). On generation start, reads `lastMood` from `EncounterManager` (stored from previous `ClueResponse`). Selects a `TheaterTypingProfile` with base speed, burst pattern, pause durations, and optional reverse-typing segments. Coroutine types each theater line according to the profile. ~100 lines, directly enhances the generation theater rather than adding a new system. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)

### Killed this pass
- NPC Signature Sound on Guess (Mood-Keyed Audio Cue) — requires nonexistent audio infrastructure (exceeds 2-day budget), redundant with visual mood signals, mood is static between clues making per-guess audio repetitive

### Resume token
LAST_PASS=8 | BEST_IDEAS=12 | NEXT_ROLE=Generator

---

## Pass 9 | 2026-03-04 16:45 | Generator
### New ideas

- **Ghost Guess (NPC Shadow Input)**: After the player submits a WRONG phrase guess, a "ghost" guess appears in the input field — a faint, semi-transparent word that the NPC "almost" typed as a counter-taunt. This ghost word is selected from the same word pool category as the target (so it's thematically adjacent but wrong), styled with the NPC's mood color, and fades out over 2 seconds. The ghost word is NOT the answer — it's the NPC playfully (or menacingly) showing they know words in the same space. Guide's ghost words are gentle near-synonyms. Trickster's ghost words are deliberately misleading (anagrams or homophones of the real answer). Whisperer's ghost words are one edit-distance away from the correct answer (cruel proximity). The ghost text uses the NPC's current mood from the last ClueResponse to set its color and fade speed (frustrated = fast angry flash, amused = slow warm linger). Implementation: `GhostGuessDisplay` component on EncounterUI. On `PhraseGuessResultEvent` where result is wrong, reads the NPC archetype from `EncounterManager.CurrentNPC` and picks a random word from the active word pool (excluding the target). Displays it in the input field as a TextMeshPro overlay with alpha=0.35, mood-colored, with a DOTween fade-out over 2s. Per-NPC display variants (Trickster scrambles the ghost word's letters, Whisperer shows it character by character). ~85 lines. | Checklist: ✅✅✅✅

- **Tile Loyalty Marks (NPC Claim Stamps)**: When a letter is guessed correctly, the tile that reveals it gets a small NPC-specific visual mark — a tiny icon or glyph in the corner of the tile that shows WHICH clue cycle the letter was revealed during. Clue #1 reveals show the NPC's initial mood icon (a small colored dot). Clue #2 reveals show a different icon (the mood may have shifted). By the end of the encounter, the solved board is a mosaic of these marks — a visual record of the encounter's emotional arc stamped directly onto the tiles. The player can glance at the completed board and see: "the first three letters came during the NPC's amused phase (amber dots), then the next two during frustration (red dots), and the final letter during a menacing phase (purple dot)." This gives the victory screen a narrative quality — the board tells the story of the encounter. Implementation: `TileLoyaltyMark` component added to tile prefab. On `LetterRevealedEvent`, reads current clue cycle number and mood from `EncounterManager`. Instantiates a small Image child in the tile's top-right corner with color from `MoodColorMap` and a cycle-number tooltip. ~65 lines + minor tile prefab modification. | Checklist: ✅✅✅✅

- **NPC Wager Reaction (Pre-Encounter Mood Seed)**: When the Confidence Bet or Gambit/Wager system (from Pillar 2) is active and the player places a high bet, the NPC's FIRST clue prompt includes the bet context: "The player has wagered heavily. They are confident." This seeds the AI's mood for the encounter — a Trickster told the player is confident will naturally respond with taunting, competitive clues; a Guide told the player is confident might be more challenging than usual. On the UI side, the moment the player confirms a bet, the NPC portrait area shows a brief reaction animation: the CRT around the NPC name flickers with a mood preview (nervous energy for high bets, calm for no bet). If the bet is maximum, the NPC name text briefly shakes (they're excited/threatened). This makes the bet feel like a dialogue with the NPC, not just a UI toggle. Implementation: `WagerReactionDisplay` component. On `BetConfirmedEvent`, reads bet level (none/low/high/max), applies a DOTween shake to the NPC name text scaled by bet level. Injects bet context into PromptBuilder's system prompt (~10 lines). CRT border flicker via `CRTSettings.noiseIntensity` spike for 0.5s. ~70 lines total. Depends on Confidence Bet system existing, but the NPC reaction layer itself is independent and under 2 days. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- Tile Loyalty Marks (NPC Claim Stamps)
- NPC Wager Reaction (Pre-Encounter Mood Seed)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=9 | BEST_IDEAS=15 | NEXT_ROLE=Critic

---

## Pass 10 | 2026-03-04 17:00 | Critic
### Analysis

The weakest idea is **Tile Loyalty Marks (NPC Claim Stamps)**. Here's why:

1. **The payoff is retrospective, not immediate.** The marks only form a meaningful pattern AFTER the encounter is mostly or fully complete — you need 4-6 revealed tiles with different mood dots before the "mosaic" reads as a narrative. During active play, the player is focused on hidden tiles and their next guess, not studying tiny corner dots on already-revealed tiles. The idea's best moment ("glance at the completed board and see the emotional arc") happens at the victory screen, which the player spends 2-3 seconds on before clicking "Continue." The 5-second feedback window barely applies.
2. **Visual clutter on the most important UI element.** The tile board is where ALL player attention goes during an encounter. Each tile is already showing a letter (or blank), possibly with Mood Pulse effects, and potentially Tile Corruption animations. Adding a small colored dot in the corner of every revealed tile creates noise on the primary gameplay surface. At small tile sizes (long words like 8-10 letters), the dots would be nearly invisible or would crowd the letter.
3. **Weak AI signal compared to alternatives.** The mood dots represent "what the mood was when this letter was revealed" — but the player doesn't control WHEN letters are revealed (it's based on correct guesses and auto-reveals from clues). So the marks don't reflect player decisions; they reflect the intersection of player guesses and NPC mood timing, which is too indirect to feel like the AI is "reacting."

**Replacement — NPC Difficulty Tell (Clue Complexity Indicator):** When a clue arrives, a small "signal complexity" indicator appears next to the clue — styled as a CRT-themed meter (1-3 bars, like a terminal signal readout: `[▮▯▯]` for simple, `[▮▮▮]` for complex). The complexity level is derived directly from the LLM response: it's parsed from the clue's word count and vocabulary level (simple heuristic: clues under 8 words with common vocabulary = 1 bar, 8-15 words = 2 bars, 15+ words or containing uncommon words = 3 bars). But here's the AI-visibility hook: the NPC's mood modulates the meter's DISPLAY. When the NPC is "encouraging," even a 3-bar clue shows the bars in green (the NPC is helping despite complexity). When "taunting," even a 1-bar clue shows bars in red with a flicker (the NPC made it simple but wants you to doubt yourself). When "cryptic," the bars render as `[▮?▯]` — one bar is replaced with a question mark (the NPC is being deliberately opaque about how helpful they're being). The player reads both the clue's inherent complexity AND the NPC's attitude toward giving it. Implementation: `ClueDifficultyMeter` component. On `ClueReceivedEvent`, counts words in `Clue.Text`, classifies into 3 tiers. Reads `Clue.Mood`, applies mood-color and mood-specific display variants to 3 UI Image bars. Positioned inline after the clue text. Uses existing `MoodColorMap`. ~65 lines. | Checklist: ✅✅✅✅

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)

### Killed this pass
- Tile Loyalty Marks (NPC Claim Stamps) — payoff is retrospective not immediate, clutters the primary tile board UI, mood dots reflect timing coincidence not player-AI interaction

### Resume token
LAST_PASS=10 | BEST_IDEAS=15 | NEXT_ROLE=Generator

---

## Pass 11 | 2026-03-04 18:00 | Generator
### New ideas

- **Mood Bargain (Mid-Encounter Deal)**: When the NPC's mood shifts to "frustrated" or "amused" (detected from `ClueResponse.Mood` on `ClueReceivedEvent`), a time-limited prompt appears at the bottom of the encounter UI: the NPC offers a deal. If frustrated: "I'll reveal a vowel if you skip your next guess" (the player loses one guess slot but gets a free vowel reveal). If amused: "Double or nothing — guess correctly on your next attempt for 2x gold, but wrong costs 2x HP." The deal disappears after 8 seconds (DOTween alpha fade) or when the player submits a guess, whichever comes first. The player must DECIDE: take the deal (tap/click the deal prompt) or ignore it and play normally. The deal text is NPC-flavored — Trickster deals are unfair (skewed risk), Guide deals are generous (low risk), Whisperer deals are cryptic ("accept and see what happens" — the effect is hidden until triggered). This forces a strategic decision CAUSED by the AI's emotional state. The player can read the mood, anticipate deals, and plan around them. Implementation: `MoodBargainSystem` component on EncounterUI. Subscribes to `ClueReceivedEvent`. On mood change to frustrated/amused (compared to `lastMood`), selects a deal from `BargainTemplates` dictionary keyed by `[archetype][mood]` (~12 templates: 4 NPCs x 3 deal-eligible moods). Displays a clickable TextMeshPro with DOTween fade-in, starts an 8-second countdown with a fill bar. On click: applies the deal effect (publish `TomeRevealRequestEvent` for reveals, set `_bonusGoldMultiplier`/`_bonusHPMultiplier` flags on EncounterManager). On timeout: deal vanishes. ~130 lines. New event: `BargainOfferedEvent`, `BargainAcceptedEvent`. | Checklist: ✅ Player sees the deal popup within 1s of clue arriving ✅ Triggers only after a guess cycle leads to a new clue ✅ Deal type depends on NPC mood (AI state drives the offer) ✅ UI component + event wiring, no new systems needed, ~1.5 days

- **NPC Adaptive Difficulty (Mercy/Cruelty Shift)**: The LLM's mood response directly adjusts encounter parameters mid-game. When the NPC mood is "frustrated" (the NPC is losing patience, player is doing poorly), the system HELPS: `lettersRevealedPerClue` increases by 1 for the next clue cycle, and the PromptBuilder appends "give an easier, more direct clue" to the system prompt. When the mood is "amused" or "excited" (player is doing well), the system CHALLENGES: `lettersRevealedPerClue` drops to 0 for the next cycle, and the prompt appends "make the clue more oblique and challenging." The player sees this on the UI as a small terminal readout next to the clue: `[SIGNAL: BOOSTED]` (green, when mercy is active) or `[SIGNAL: DEGRADED]` (red, when cruelty is active). This creates a rubber-banding difficulty system where the AI's OWN mood is the tuning knob. A smug NPC makes the game harder. A struggling NPC accidentally helps you. The player feels the AI pushing back or yielding. Implementation: `AdaptiveDifficultyMod` component. Subscribes to `ClueReceivedEvent`. Reads mood, stores a `_difficultyShift` enum (Mercy/Normal/Cruel). On next `RequestNextClue`, EncounterManager reads the shift to temporarily modify `lettersRevealedPerClue`. PromptBuilder gets a new optional `difficultyHint` parameter appended to the user message. `EncounterUI` displays the signal label. ~90 lines across 3 files (AdaptiveDifficultyMod ~40, EncounterManager +20, PromptBuilder +15, EncounterUI +15). | Checklist: ✅ Signal label appears instantly with clue ✅ Activates based on guess-driven clue cycles ✅ Mood from LLM directly controls difficulty parameters ✅ Modifying existing config values + small UI label, ~1 day

- **Spell Streak (Combo Momentum)**: When the player guesses 3+ correct letters in a row WITHOUT a miss (tracked via `GuessSubmittedEvent` where `IsLetterInPhrase` is true), a "spell streak" activates. The streak has visible, escalating effects: at streak 3, the tile board tiles pulse faster (ties into Mood Pulse Tiles); at streak 5, the CRT scanline intensity drops to near-zero (the screen "clears" — the NPC is losing control); at streak 7, the next clue is automatically upgraded — PromptBuilder receives a "the player is on a hot streak, make your next clue reveal more" instruction, AND 2 bonus letters auto-reveal. The streak counter is displayed as a glowing number in the corner of the encounter UI, styled as a terminal counter: `STREAK: 5▮`. On any miss (wrong letter or wrong phrase), the streak resets to 0 with a CRT glitch (noiseIntensity spike via CRTSettings). The NPC's response to the streak is visible: PromptBuilder tells the LLM "the player has guessed [N] correct letters in a row," which naturally shifts the mood (Trickster gets frustrated, Guide gets excited). Implementation: `SpellStreakTracker` component. Subscribes to `GuessSubmittedEvent`. Increments `_streakCount` on letter hits, resets on misses. At thresholds (3/5/7), publishes `StreakThresholdEvent` carrying the level. `EncounterUI` updates the streak counter display. `CRTSettings` responds to threshold events for visual shifts. PromptBuilder receives streak count as context. ~100 lines across SpellStreakTracker (~50), EncounterUI (+20), PromptBuilder (+10), CRTSettings (+20). | Checklist: ✅ Streak counter updates instantly per guess ✅ Each letter guess feeds the streak ✅ Streak count injected into LLM prompt shifts NPC mood/behavior ✅ Event-driven, counter UI + threshold triggers, ~1.5 days

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Board Heartbeat (AI Breathing)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=11 | BEST_IDEAS=18 | NEXT_ROLE=Critic

---

## Pass 12 | 2026-03-04 18:30 | Critic
### Analysis

Time to be aggressive. 18 ideas is too many — the list needs pruning. I'm looking for: overlap between ideas, cosmetic-only effects that don't change player behavior, and ideas where the "AI visibility" claim is weak on closer inspection.

The weakest idea is **Board Heartbeat (AI Breathing)**. Here's the case for killing it:

1. **It doesn't change player behavior AT ALL.** The board tiles pulsing at a rate tied to the NPC's mood is purely ambient animation. There is no decision the player makes differently because the tiles are pulsing at 0.8Hz vs 1.2Hz. Compare to Mood Bargain, which forces a strategic choice, or Spell Streak, which rewards a play pattern. Board Heartbeat is wallpaper. A player watching tiles "breathe" faster doesn't think "I should change my strategy" — they think "the UI is animated."

2. **It overlaps with THREE other ideas.** Mood Pulse Tiles already colors tiles based on mood. Mood Glitch Transition already shifts CRT parameters on mood change. Tile Corruption already animates tiles based on mood. Adding a FOURTH tile-level mood animation creates visual noise with diminishing returns. The tile board is doing too much. Each additional animation competes for attention and makes every individual effect harder to notice.

3. **The "AI state" connection is the weakest of all surviving ideas.** The breathing rate maps mood to a pulse frequency. But mood is a single string ("neutral", "amused", etc.) parsed from the LLM response. Mapping a string to a float and driving an animation loop is the most shallow possible AI integration. NPC Grudge Memory changes what the LLM SAYS. Adaptive Difficulty changes the game's RULES. Board Heartbeat changes... the sine wave frequency on a UI animation. It's technically AI-driven but practically invisible.

**Replacement — NPC Bluff Clue (Deception Mechanic):** On specific NPCs (Trickster, Whisperer), when the NPC's mood is "amused" or "cryptic," there is a 30% chance the clue display adds a visual "distortion tell" — the clue text renders with a subtle red underline for 1 second that then fades, indicating the NPC might be bluffing (giving a misleading clue). This is NOT a lie — the LLM always gives honest clues — but it plants DOUBT. The player must decide: "Do I trust this clue, or is the NPC messing with me?" This changes behavior because a suspicious player might switch from phrase-guessing to safer letter-guessing when they see the red underline. The Guide NEVER bluffs (no underline ever appears). The Riddlemaster bluffs rarely (10% on "cryptic" mood only). The Trickster bluffs often (30% on "amused" or "cryptic"). The Whisperer's bluff tell is different — instead of a red underline, the clue text briefly renders BACKWARDS for 0.5 seconds before flipping to normal (disorienting, thematic). Whether a bluff tell appears is logged but doesn't change the actual clue content — it's psychological warfare. Implementation: `NPCBluffTell` component. On `ClueReceivedEvent`, reads NPC archetype and mood. Rolls against a `BluffChanceMap[archetype][mood]` dictionary. If triggered, applies a DOTween sequence to the clue TextMeshPro: red underline fade for Guide/Riddlemaster/Trickster (using `TextMeshPro.fontSharedMaterial` underline color), or a scale-X flip animation for Whisperer. ~75 lines. No gameplay changes, no LLM changes — purely a psychological UI layer. | Checklist: ✅ Red underline/flip appears within 1s of clue arriving ✅ Player's reaction to the bluff tell influences their guess mode choice ✅ Bluff chance is gated by NPC archetype AND AI-returned mood ✅ DOTween animation on existing text, ~0.5 days

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)

### Killed this pass
- Board Heartbeat (AI Breathing) — purely cosmetic with zero behavior change, overlaps with 3 other tile-level mood effects (Mood Pulse Tiles, Mood Glitch Transition, Tile Corruption), shallowest AI connection of all surviving ideas

### Resume token
LAST_PASS=12 | BEST_IDEAS=18 | NEXT_ROLE=Generator

---

## Pass 13 | 2026-03-04 19:00 | Generator
### New ideas

- **Letter Sacrifice (Strategic Tile Trade)**: The player can voluntarily SACRIFICE a correctly revealed letter tile — clicking a revealed tile and confirming — to force the NPC to give an immediate bonus clue. The sacrificed tile re-hides (flips back to blank on the board via `BoardState`), the player loses that information, but receives an instant `RequestNextClue()` call with a PromptBuilder injection: "The player sacrificed a known letter to demand a better clue. Respond with a significantly more helpful hint." The NPC's personality flavors the response: the Guide gives a genuinely better clue, the Trickster gives a clue that's technically more specific but phrased to mislead, the Whisperer gives one that's better but only by one word (grudging). On the UI, the sacrificed tile plays a shatter animation (DOTween scale-to-zero + particle burst via a simple UI particle) and the NPC name flashes (the NPC is "surprised" — mood shifts toward "amused" or "excited"). This creates a real strategic decision: "I have 6 of 9 letters revealed. If I sacrifice one, I get a better clue, but now I have 5 of 9 and need to re-earn that letter." It's especially powerful for phrases where one letter might appear in multiple tiles. Implementation: `LetterSacrificeSystem` component. On tile click (existing tile click handler), if tile is revealed AND sacrifice mode is active (toggled by a small "sacrifice" button), calls `BoardState.RehideTile(index)` (new method: sets `TileState` back to Hidden, ~5 lines). Publishes `LetterSacrificedEvent`. EncounterManager subscribes, calls `RequestNextClue()` with a sacrifice flag. PromptBuilder appends the sacrifice context. TileBoardUI plays shatter animation on the re-hidden tile. ~120 lines across 4 files (LetterSacrificeSystem ~40, BoardState +10, EncounterManager +25, PromptBuilder +15, TileBoardUI +30). | Checklist: ✅ Tile shatters + new clue arrives within 2-3s ✅ Player deliberately gives up a revealed letter (word mechanic input) ✅ NPC personality shapes the quality/style of the sacrifice-triggered clue ✅ New BoardState method + event wiring + animation, ~1.5 days

- **NPC Vocabulary Echo (Word Pattern Feedback)**: After every 2nd wrong phrase guess, the NPC "echoes" back a transformed version of the player's guess as a one-line terminal message, styled differently per NPC archetype. The echo uses the player's wrong guess as input and the NPC's current mood to shape the response. The Guide echoes with encouragement: if the player guessed "elephant" and the answer is "engineer," the echo reads `> Close family, wrong branch.` The Trickster echoes with mockery: `> Ha! Not even the same continent.` The Riddlemaster echoes with a meta-riddle: `> Your word walks on four legs. Mine walks on two.` These echoes are NOT LLM-generated — they're selected from a curated bank of ~30 templates per NPC, with mood variants. The template selection uses the Levenshtein distance between guess and answer (close = "warm" templates, far = "cold" templates) combined with the current mood. The echo appears below the clue in a dimmer monospace style (same slot as Mood Shift Narration, sharing that UI space but triggered by different events — phrase miss vs mood change). Implementation: `VocabularyEchoSystem` component. On `GuessSubmittedEvent` where `GuessType == Phrase` and `IsCorrect == false`, increments a counter. On every 2nd miss, computes Levenshtein distance (static helper, ~15 lines), classifies as warm/cold, selects from `EchoTemplateBank` ScriptableObject keyed by `[archetype][mood][warmth]`. Displays via the same fading TextMeshPro slot used by Mood Shift Narration. ~85 lines + SO data (~30 templates per NPC, ~120 strings total in the asset). | Checklist: ✅ Echo appears instantly after the 2nd wrong phrase guess ✅ Triggered by the player's phrase guess (spelling input) ✅ NPC archetype + current AI mood + guess proximity all shape the echo ✅ Template bank + Levenshtein helper + shared UI slot, ~1 day

- **Mood Lock (Emotional Pinning via Streaks)**: When the player achieves a 3+ Spell Streak (from Pass 11), the NPC's mood becomes "locked" — the PromptBuilder injects "Your mood is locked to [current_mood]. You MUST keep this mood in your response." into the system prompt for the next 2 clue cycles. The UI shows a small padlock icon next to the mood indicator (NPC Patience Meter or wherever mood is displayed), confirming the lock. Why this matters strategically: if the NPC's mood is "frustrated" when the streak hits 3, locking it means the NPC stays frustrated, which (combined with Adaptive Difficulty) triggers mercy mode — more letters revealed, easier clues. If the mood is "amused" at streak 3, locking it keeps the NPC entertained, which (combined with Mood Bargain) increases the chance of favorable deals. The player can TIME their streak to lock a desirable mood. Conversely, if the streak breaks (miss) while a lock is active, the lock shatters with a CRT glitch AND the NPC's mood forcibly shifts to the opposite emotional register — frustrated becomes amused, amused becomes menacing. The punishment for breaking a locked streak is worse than a normal miss. This creates a high-risk-high-reward system around the streak mechanic. Implementation: `MoodLockSystem` component. Subscribes to `StreakThresholdEvent` (from Spell Streak). On streak >= 3, sets `_isMoodLocked = true` and `_lockDuration = 2`. On `ClueReceivedEvent`, decrements `_lockDuration`; when it hits 0, publishes `MoodUnlockedEvent`. On streak break (`StreakBrokenEvent`), if locked, publishes `MoodShatterEvent` with the opposite mood. PromptBuilder reads lock state and appends the mood constraint. UI shows padlock icon via DOTween scale-in. ~80 lines across MoodLockSystem (~50), PromptBuilder (+15), EncounterUI (+15). | Checklist: ✅ Padlock icon appears instantly on streak threshold ✅ Streak is built from correct letter guesses (spelling input) ✅ Lock constrains the LLM's mood output, shatter forces a mood shift — both directly alter AI behavior ✅ Event subscriptions + prompt injection + icon display, ~1 day

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Letter Heat Map (NPC Suspicion Glow)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=13 | BEST_IDEAS=21 | NEXT_ROLE=Critic

---

## Pass 14 | 2026-03-04 19:30 | Critic
### Analysis

21 ideas. Time to be ruthless. I'm looking at the full surviving list through three lenses: (1) does it change how the player PLAYS, or is it just visual seasoning? (2) does it overlap with another idea to the point where only one should survive? (3) is the AI connection genuine, or is it a mood-string-to-color lookup pretending to be AI integration?

The weakest idea is **Letter Heat Map (NPC Suspicion Glow)**. Here's why it must die:

1. **It's a retrospective color palette, not a gameplay mechanic.** The guessed-letters panel is a reference display — the player glances at it to remember "which letters have I tried." Coloring wrong letters by mood adds visual information, but what does the player DO with that information? Nothing. "I missed 'X' when the NPC was amused (amber) and 'T' when the NPC was frustrated (red)" doesn't inform any future decision. The player doesn't think "the NPC was frustrated on my last miss, so I should guess vowels now." The heat map is a mood HISTORY, not a mood SIGNAL. By the time you can see the pattern, the encounter is half over and the moods have already shifted.

2. **It overlaps fatally with NPC Patience Meter and Reactive Clue Intensity.** Both of those already communicate the NPC's current mood visually — the Patience Meter as a direct bar/indicator, Reactive Clue Intensity via text speed and CRT noise. Letter Heat Map adds a THIRD mood visualization that's less readable (scattered colored letters vs. a clear bar) and less actionable (historical vs. current). Three systems showing mood is two too many — Patience Meter and Reactive Clue Intensity cover it better.

3. **The MoodColorMap dependency makes it a satellite of Mood Pulse Tiles.** The idea description explicitly says it reuses the MoodColorMap from Mood Pulse Tiles. That means it's not an independent system — it's a cosmetic extension of Mood Pulse Tiles applied to a different UI element. If we're already coloring the TILES by mood (Mood Pulse Tiles), coloring the GUESSED LETTERS panel by mood is the same signal on a different surface. Redundant.

**Replacement — NPC Trap Tile (Decoy Board Hazard):** When the NPC's mood is "menacing" or "cryptic" (from `ClueResponse.Mood`), the NPC can "plant" a trap on the board: one random HIDDEN tile gets a subtle visual tell — a faint red border or hairline crack in the tile (different per NPC archetype). If the player guesses the letter that would reveal that trapped tile, the reveal still happens (correct gameplay is never altered), BUT the NPC "captures" the guess — the player's next clue is DELAYED by one additional guess cycle (they must submit another guess before the next clue arrives, effectively losing a clue). The player can see the trap (the tell is subtle but visible if they look) and choose to avoid that region of the board by guessing letters they suspect are in OTHER positions. This creates board-reading strategy: the player studies the tile layout, notices the trapped tile's position, tries to deduce which letter it represents, and avoids guessing that letter — or deliberately triggers it if they're confident they can win without the next clue. The trap only exists for one clue cycle — if the player doesn't trigger it, it fades on the next `ClueReceivedEvent`. Only Trickster, Whisperer, and Riddlemaster can set traps; Guide never traps. Implementation: `NPCTrapTile` component. On `ClueReceivedEvent`, if mood is menacing/cryptic and NPC archetype allows traps, selects a random hidden tile index, publishes `TrapPlacedEvent` with the tile index. `TileBoardUI` subscribes and applies a red border overlay (Image child with DOTween pulse). On `LetterRevealedEvent`, checks if any revealed position matches the trapped tile; if so, publishes `TrapTriggeredEvent`. EncounterManager subscribes to `TrapTriggeredEvent` and sets a `_skipNextClue` flag, so the next `SubmitGuess` miss path skips the `RequestNextClue()` call. On next `ClueReceivedEvent`, clears the trap. ~110 lines across NPCTrapTile (~45), TileBoardUI (+25), EncounterManager (+20), new events (+20). | Checklist: ✅ Red border appears on trapped tile within 1s of clue ✅ Player's letter guesses interact with the trap (avoid or trigger) ✅ Traps only appear based on NPC mood (AI state) and archetype ✅ Overlay + event wiring + one flag in EncounterManager, ~1.5 days

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)

### Killed this pass
- Letter Heat Map (NPC Suspicion Glow) — retrospective mood history with zero behavior change, triply redundant with Patience Meter + Reactive Clue Intensity + Mood Pulse Tiles, satellite of MoodColorMap not an independent system

### Resume token
LAST_PASS=14 | BEST_IDEAS=21 | NEXT_ROLE=Generator

---

## Pass 15 | 2026-03-04 20:00 | Generator
### New ideas

- **NPC Handoff (Multi-NPC Encounter)**: In encounters from node 4 onward, when the NPC's mood shifts to "frustrated" (they're struggling to stump the player), there's a 40% chance a SECOND NPC "takes over" the clue channel. The EncounterManager swaps the active `NPCPromptData` to a different archetype from the `regularNPCs` array, and the UI shows a terminal-style handoff: `> [Riddlemaster] has disconnected.` / `> [Trickster] is now online.` The new NPC generates the next clue with their own personality, mood, and style. The handoff is jarring — the visual treatment shifts (Mood Glitch Transition fires, NPC name changes, CRT tint shifts to the new NPC's palette). The player was reading the Riddlemaster's patterns and suddenly faces the Trickster's misdirection. This also means Mood Lock, Streaks, and Traps now interact with a DIFFERENT NPC personality mid-encounter. A mood locked to "amused" on the Riddlemaster carries over but manifests differently under the Trickster's archetype. The handoff is one-way (the original NPC doesn't return in this encounter) and can only happen once per encounter. Implementation: `NPCHandoffSystem` component. Subscribes to `ClueReceivedEvent`. If mood == "frustrated" AND encounter node >= 4 AND `_handoffUsed == false`, rolls 40%. On trigger: selects a different NPC from `GameManager.regularNPCs` (excluding current), updates `EncounterManager._npcData` via a new public method `SwapNPC(NPCPromptData)`. Publishes `NPCHandoffEvent` with old and new NPC names. EncounterUI subscribes to show the handoff text sequence (DOTween sequence: fade old name out, show disconnect/connect messages, fade new name in). ~100 lines across NPCHandoffSystem (~45), EncounterManager (+15 for SwapNPC method), EncounterUI (+40 for handoff animation). | Checklist: ✅ Disconnect/connect terminal messages appear within 2s of clue ✅ Triggers during clue cycle caused by player's guess performance ✅ New NPC brings entirely different AI personality/prompt template ✅ One new method + component + UI animation, ~1.5 days

- **Vowel Gambit (Risk-Reward Letter Category)**: The player can toggle a "Vowel Gambit" mode (small toggle button on the encounter UI, styled as a terminal switch: `[VOWEL LOCK: OFF]` / `[VOWEL LOCK: ON]`). When active, the player's next letter guess is CONSTRAINED to vowels only (A, E, I, O, U) — consonant guesses are rejected with a terminal error message. In exchange: if the vowel hits (is in the word), it reveals ALL instances of that vowel AND one additional random hidden tile as a bonus. If the vowel misses, HP loss is doubled (from `hpLostPerWrongLetter` * 2). The NPC REACTS to the gambit: when Vowel Lock is active, PromptBuilder injects "The player has activated Vowel Lock — they're going all-in on vowels" into the prompt. The LLM naturally adjusts — a Trickster might shift mood to "amused" (they love seeing the player take risks), while a Guide might shift to "encouraging" (supporting the bold play). The NPC's mood response to the gambit flows back through all mood-dependent systems (Mood Bargain, Adaptive Difficulty, Trap Tiles, etc.), creating a cascade of AI reactions from a single player toggle. Implementation: `VowelGambitToggle` component on EncounterUI. Toggle button swaps `_vowelLockActive` bool. On `SubmitGuess`, if vowel lock is active and guess is a consonant, rejects with feedback (no EncounterManager involvement). If vowel lock is active and guess is a vowel, passes a `vowelGambit: true` flag to EncounterManager, which doubles the `RevealRandomHidden` call (bonus reveal) on hit or doubles HP loss on miss. PromptBuilder receives `_vowelGambitActive` and appends context. Toggle resets to OFF after one guess (single-use per activation). ~90 lines across VowelGambitToggle (~35), EncounterManager (+25), PromptBuilder (+10), EncounterUI (+20 for toggle button). | Checklist: ✅ Toggle state + rejection/bonus feedback instant ✅ Constrains and enhances letter guessing (core mechanic) ✅ NPC reacts to gambit via prompt injection, mood shifts cascade through all mood systems ✅ Toggle button + flag checks + prompt line, ~1 day

- **Clue Decay (Timed Information Erosion)**: When the NPC's mood is "menacing" or "frustrated" (hostile moods), the clue text doesn't just sit static — it DECAYS over time. Starting 6 seconds after the clue appears, individual characters in the clue text begin to be replaced by `_` characters at a rate of one character per second, right-to-left. The player must READ and INTERNALIZE the clue quickly before it erodes. When the clue is fully decayed (all characters replaced), it stays as a line of underscores — the player can see WHERE the clue was and how long it was, but the content is gone. On non-hostile moods ("neutral", "amused", "encouraging"), the clue persists indefinitely — no decay. On "cryptic" mood, the decay is slower (one character every 2 seconds) and goes from the MIDDLE outward (the edges of the clue survive longest, removing the core meaning first). The player can counter the decay by guessing correctly: each correct letter guess "refreshes" the clue, restoring all characters and resetting the decay timer. This turns the encounter into a race against the NPC's hostility. If you're doing well (correct guesses), the clue stays readable. If you're struggling (no correct guesses), the hostile NPC's clue crumbles. The visual effect uses TextMeshPro's character-level manipulation (`TMP_TextInfo.characterInfo`), no shader changes. Implementation: `ClueDecaySystem` component. On `ClueReceivedEvent`, reads mood. If hostile, starts a coroutine that iterates through `TMP_Text.textInfo.characterInfo` and sets `character.isVisible = false` (or replaces with `_` via string manipulation) at the configured rate. On `LetterRevealedEvent` where source == "guess", resets the timer and restores full text. Mood-to-decay-profile mapping in a small dictionary (hostile = 1char/sec R-to-L, cryptic = 1char/2sec middle-out, friendly = no decay). ~85 lines. | Checklist: ✅ Decay begins 6s after clue (visible within feedback window), refresh is instant on correct guess ✅ Correct letter guesses counteract the decay (spelling input as defense) ✅ Decay only activates on hostile AI moods, decay pattern varies by mood ✅ Coroutine + TMP character manipulation, no new systems, ~1 day

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=15 | BEST_IDEAS=24 | NEXT_ROLE=Critic

---

## Pass 16 | 2026-03-04 21:00 | Critic
### Analysis

24 ideas. That is far too many. A focused game needs 12-15 strong mechanics, not 24 mediocre ones. I'm hunting for three things: (1) ideas that are purely decorative — the player wouldn't change a single guess because of them, (2) ideas that overlap so heavily with a stronger sibling that only one deserves to survive, and (3) ideas where the "AI visibility" claim is a thin veneer over local-only computation.

The weakest idea is **NPC Stutter on Near-Miss (Phonetic Proximity Feedback)**. Here's the case for execution:

1. **The "AI state" connection is cosmetic, not causal.** The core mechanic is a hardcoded `Dictionary<char, char[]>` of alphabetically/phonetically adjacent letters checked locally against `BoardState.TargetPhrase`. The AI contributes nothing to the near-miss detection — it's pure client-side string comparison. The NPC personality merely changes the stutter animation (gentle pause vs. exaggerated backspace), which is a cosmetic skin, not an AI-driven behavior. Strip the NPC personality layer and the idea is identical: "vibrate the text when the player guesses a neighboring letter." Compare to NPC Adaptive Difficulty, where the AI's mood literally changes the game's difficulty parameters, or Mood Bargain, where the AI's mood creates strategic choices. The stutter is decoration pretending to be AI integration.

2. **The phonetic adjacency model is linguistically fragile and culturally arbitrary.** The idea claims "B" is near "P" and "N" is near "M." But English phonetic proximity is not a simple 26-pair map — it depends on place and manner of articulation, which varies by accent and language. Romanian (which this game supports!) has different phonetic clusters. The hardcoded dictionary would be wrong for Romanian players, misleading for ESL players, and opaque to anyone who doesn't know phonetics. A "near-miss" that triggers on "B" when the answer contains "P" will confuse most players rather than helping them — they'll think "B" was partially correct, which it isn't.

3. **It conflicts with the existing feedback model.** The game already has clear binary feedback: a letter is either in the word or it isn't. Introducing a "you were close" signal muddies this. In Wheel of Fortune, there's no "warm/cold" on letter guesses — you hit or you miss. Adding proximity feedback changes the information model from binary to gradient, which cascades into confusion: "If B stuttered, does that mean P is definitely in the word? Can I rely on that?" The answer is "sometimes, depending on the hardcoded map," which is a terrible answer for a player trying to form strategy.

**Replacement — NPC Endgame Monologue (Victory/Defeat Personality Cascade):** When the encounter ends — win or lose — the NPC delivers a 1-2 sentence personality-flavored response that reflects the ENTIRE encounter arc. The response is generated by a fast LLM call (or selected from a curated template bank) using the encounter's aggregate data: total guesses used, HP lost, which clues were given, the NPC's mood sequence across the encounter, and whether the player used any special mechanics (sacrifice, gambit, streak). On WIN, the Guide says something warm: "You found it in three guesses. I barely got to warm up." The Trickster says something bitter: "Fine. Take your gold. Next time I won't be so generous with my riddles." On LOSS, the Whisperer's text types agonizingly slowly: "So... close... The word was right there... and you... couldn't... reach it." The text appears in the encounter result overlay, replacing the current generic "Victory!" / "Defeat" message. The NPC's FINAL mood (from their last clue) determines the tone: a frustrated Riddlemaster on a player win reacts differently than an amused one. This makes the AI visible at the most emotionally charged moment of every encounter — the resolution. Players REMEMBER the last thing an NPC said. Implementation: `NPCEndgameMonologue` component. On `EncounterEndedEvent`, reads `EncounterResult` (win/loss, guesses used, HP lost) and `lastMood` from EncounterManager. Selects from `EndgameTemplateBank` ScriptableObject keyed by `[archetype][outcome][lastMood]` (~60 curated strings: 5 NPCs x 2 outcomes x ~6 moods). Displays via the result panel's existing text area with the NPC's typewriter profile (from Reactive Clue Intensity). Optional: if LLM is available and response time is fast enough (<2s), fire a lightweight generation with a short prompt instead of template. ~80 lines + SO data asset. | Checklist: ✅ Monologue appears immediately at encounter end ✅ Encounter outcome is determined by the player's word-guessing performance ✅ NPC archetype + final AI mood + encounter stats shape the response ✅ Template bank + result panel text swap + typewriter reuse, ~1 day

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)
- NPC Endgame Monologue (Victory/Defeat Personality Cascade)

### Killed this pass
- NPC Stutter on Near-Miss (Phonetic Proximity Feedback) — AI connection is cosmetic (near-miss is local string comparison, not AI-driven), phonetic adjacency model is linguistically fragile and wrong for Romanian, introduces gradient feedback that conflicts with the game's binary hit/miss information model

### Resume token
LAST_PASS=16 | BEST_IDEAS=24 | NEXT_ROLE=Generator

---

## Pass 17 | 2026-03-04 21:30 | Generator
### New ideas

These ideas explore genuinely new territory — mechanics that create stories, break expectations, and make the AI's choices feel like an adversary (or ally) with agency.

- **NPC Word Veto (AI Refuses a Word)**: At the start of certain encounters (wave 2+, non-boss), there's a 25% chance the NPC "rejects" the randomly selected word and forces a reroll. The UI shows this live: `> WORD SELECTED: ████████` / `> [Trickster] REJECTED. TOO EASY.` / `> REROLLING...` / `> WORD SELECTED: ████████` / `> [Trickster] ACCEPTED.` The replacement word is drawn from a harder pool (difficulty +1 from the encounter's base). The NPC's prompt includes: "You rejected the first word as too easy. You chose a harder one. You are smug about this." This seeds the AI's attitude for the entire encounter. On the UI side, the veto sequence plays as a 3-second terminal animation before the first clue, and the encounter header shows a `[VETOED]` tag. The player learns to dread certain NPCs vetoing — the Trickster vetoes frequently (35%), the Riddlemaster sometimes (25%), the Guide never (0%), the Whisperer always vetoes silently (no animation, the player just faces a harder word without knowing it was swapped — they figure this out over multiple runs). This creates a story: "The Trickster vetoed my word AGAIN and I got a 10-letter nightmare." Implementation: `NPCWordVeto` component. On encounter setup (before first clue), rolls veto chance from `VetoChanceMap[archetype]`. If triggered, calls `GameManager.GetRandomWord()` again with difficulty +1, replaces `EncounterManager._targetPhrase`. Plays the terminal animation sequence via a coroutine with DOTween-typed text lines. Whisperer variant: skip animation, just silently swap. Injects veto context into PromptBuilder. ~95 lines across NPCWordVeto (~55), EncounterManager (+15 for word swap method), PromptBuilder (+10), EncounterUI (+15 for header tag). | Checklist: ✅ Veto animation plays in 3s before encounter starts ✅ The swapped word IS the spelling challenge — directly alters what the player guesses ✅ NPC archetype determines veto chance, prompt injection shapes AI personality for the encounter ✅ Word reroll + terminal animation + prompt injection, ~1 day

- **Clue Bidding (Gold-for-Quality Trade)**: Before each clue (after clue #1), the player can spend gold to "bribe" the NPC for a better clue. A small UI prompt appears: `> [NPC] WILL ELABORATE FOR [X] GOLD. [PAY] [DECLINE]`. The cost scales with NPC hostility: Guide charges 2g, Riddlemaster charges 4g, Trickster charges 6g (and the clue quality improvement is unreliable — sometimes the Trickster takes the gold and gives a worse clue, injected via prompt: "The player paid you but you feel like being difficult"). Whisperer charges 8g but always delivers. The bribe amount is also mood-modulated: if the NPC is "frustrated," the price drops by 1g (they want to end this). If "amused," the price rises by 1g (they're enjoying watching you struggle and will charge a premium). The PromptBuilder injection for a paid clue: "The player paid [X] gold for a better clue. Give a more direct, helpful hint than you normally would." (Or for Trickster: "The player paid you. You MAY give a better hint. Or not.") On the UI, paying triggers a terminal animation: `> TRANSACTION: -[X]g` with a CRT noise spike (money changing hands distorts the signal). Implementation: `ClueBiddingSystem` component. After clue #1 lands, shows a bid prompt UI element. On pay click, deducts gold via `RunManager.SpendGold()`, sets a `_bribePaid` flag + amount on EncounterManager. Next `RequestNextClue()` call reads the flag and passes it to PromptBuilder, which appends the bribe context. Bid cost computed from `BaseBidCost[archetype] + MoodCostModifier[mood]`. Trickster unreliability: 30% chance the prompt says "ignore the bribe" instead. ~100 lines across ClueBiddingSystem (~50), EncounterManager (+15), PromptBuilder (+15), RunManager (+5 for SpendGold call), EncounterUI (+15 for bid prompt). | Checklist: ✅ Bid prompt appears instantly after clue, transaction animation on pay ✅ The player trades gold to get a better clue about the word they're guessing ✅ Bid cost varies by NPC archetype + AI mood, prompt injection shapes clue quality ✅ UI prompt + gold deduction + prompt flag, ~1.5 days

- **NPC Grudge Escalation (Cumulative Run Tension)**: An extension of NPC Grudge Memory that creates a MECHANICAL consequence, not just a prompt flavor change. The `NPCMemoryTracker` (from Grudge Memory) now tracks a cumulative `grudgeLevel` (0-3) per NPC archetype across the run. Each failed encounter with that NPC archetype adds +1 grudge. Each success resets to 0. At grudge level 1: the NPC's name in the encounter header has a faint red tint (visual warning). At grudge level 2: the NPC starts with an extra trap tile placed automatically (ties into NPC Trap Tile), AND the PromptBuilder injects "You deeply distrust this player. Your clues should be more cryptic than usual." At grudge level 3: the NPC's encounter becomes a mini-boss — word difficulty +1, max guesses -1, BUT gold reward is doubled (the NPC has put up a bigger bounty as a challenge). The UI shows grudge level as terminal text in the encounter header: `[GRUDGE: ██░]` (2 of 3 filled). Players who keep failing against the Trickster watch the grudge meter climb and know the next Trickster encounter will be brutal — creating anticipatory dread and motivation to improve. Clearing a grudge (winning at level 2-3) triggers a satisfying `> GRUDGE CLEARED` terminal message with a CRT "sigh" (noise drops to zero for 0.5s — relief). Implementation: `GrudgeEscalation` extends the existing `NPCMemoryTracker` concept. Stores `Dictionary<string, int> _grudgeLevels` in RunManager (run-scoped). On encounter end, increments on loss, resets on win. On encounter start, reads grudge level and applies effects: level 2 publishes `TrapPlacedEvent` (reuses NPC Trap Tile), level 3 modifies `EncounterManager._maxGuesses` and word difficulty. PromptBuilder reads grudge level for prompt injection. EncounterUI displays grudge meter. ~95 lines across GrudgeEscalation (~40), RunManager (+15 for grudge storage), EncounterManager (+15 for difficulty mods), PromptBuilder (+10), EncounterUI (+15 for meter display). | Checklist: ✅ Grudge meter visible in encounter header immediately ✅ Grudge accumulates from failed word-guessing encounters ✅ Grudge level alters NPC prompt (AI clue quality) + adds trap tiles (AI-driven hazard) ✅ Counter + conditional effects + UI meter, ~1.5 days

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Mood Shift Narration (Terminal Aside)
- NPC Typing Hesitation (Live Generation Tells)
- Ghost Guess (NPC Shadow Input)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- NPC Difficulty Tell (Clue Complexity Indicator)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)
- NPC Endgame Monologue (Victory/Defeat Personality Cascade)
- NPC Word Veto (AI Refuses a Word)
- Clue Bidding (Gold-for-Quality Trade)
- NPC Grudge Escalation (Cumulative Run Tension)

### Killed this pass
- (none — all three pass checklist)

### Resume token
LAST_PASS=17 | BEST_IDEAS=27 | NEXT_ROLE=Critic

---

## Pass 18 | 2026-03-04 22:00 | Critic
### Analysis

27 ideas. This is spiraling. Time to kill aggressively. I'm looking for three kills this pass, not one. The criteria are brutal: does this idea survive if I can only ship 12 features? If not, it dies.

**Kill #1: Ghost Guess (NPC Shadow Input).** The ghost word is picked randomly from the word pool — it's NOT generated by the AI. The NPC "personality" is just a display variant (Trickster scrambles it, Whisperer shows it slowly). The core mechanic is "show a random wrong word from the same category after a wrong phrase guess." This is noise, not signal. The player can't act on it — the ghost word is explicitly NOT the answer, so it provides zero strategic information. It's a decorative flourish that fires on a penalty event (wrong phrase guess) when the player is already frustrated. Adding visual clutter to a failure moment doesn't make the AI visible; it makes the failure more annoying. The mood-color tinting is identical to what Mood Pulse Tiles and Reactive Clue Intensity already do better. Dead.

**Kill #2: NPC Difficulty Tell (Clue Complexity Indicator).** The "complexity" is measured by word count and vocabulary level — a local heuristic, not an AI output. The mood modulation (coloring bars green vs. red) is cosmetic. What does the player DO with "this clue is 2 bars complex"? Nothing — they were going to read the clue and guess regardless. A 3-bar clue and a 1-bar clue require the same player action: interpret and guess. The bars don't change strategy. Compare to Mood Bargain (forces a decision), Adaptive Difficulty (changes game parameters), or Clue Bidding (gold-for-quality trade). The Difficulty Tell is an information display with no decision attached. Dead.

**Kill #3: Mood Shift Narration (Terminal Aside).** This fires only when the mood CHANGES between consecutive clues — which might happen 1-2 times per encounter, or zero times if the mood is stable. The one-line narrative ("The Riddlemaster's smile fades") is flavor text from a curated bank of ~40 strings. It doesn't change gameplay, doesn't create decisions, and doesn't alter the AI's behavior. It's a mood-transition flavor caption. It overlaps with Mood Glitch Transition (which already signals mood changes via CRT effects) and is strictly weaker: the CRT glitch is visceral and instant; a fading text line is ignorable. With Reactive Clue Intensity AND Mood Glitch Transition AND NPC Patience Meter all already communicating mood, a fourth mood-visualization system has negative marginal value — it dilutes attention from the stronger signals. Dead.

**No replacement for the three kills.** We are at 24 ideas, which is still too many, but the remaining ideas are materially stronger. Replacing three killed ideas with three new ones defeats the purpose of pruning. The list needs to SHRINK.

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- NPC Typing Hesitation (Live Generation Tells)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)
- NPC Endgame Monologue (Victory/Defeat Personality Cascade)
- NPC Word Veto (AI Refuses a Word)
- Clue Bidding (Gold-for-Quality Trade)
- NPC Grudge Escalation (Cumulative Run Tension)

### Killed this pass
- Ghost Guess (NPC Shadow Input) — random word from pool is not AI-generated, provides zero strategic information, adds visual clutter to failure moments, mood-color tinting redundant with Mood Pulse Tiles + Reactive Clue Intensity
- NPC Difficulty Tell (Clue Complexity Indicator) — complexity measured by local heuristic not AI output, bars don't change player strategy (read and guess regardless), no decision attached to the information
- Mood Shift Narration (Terminal Aside) — fires rarely (1-2 times per encounter or zero), flavor text with no gameplay impact, strictly weaker than Mood Glitch Transition at communicating mood shifts, fourth mood-visualization system dilutes attention from stronger signals

### Resume token
LAST_PASS=18 | BEST_IDEAS=24 | NEXT_ROLE=Generator

---

## Pass 19 | 2026-03-04 22:30 | Generator
### New ideas

Last generator pass. These need to be the strongest ideas yet — mechanics that create moments players screenshot, tell friends about, and build their entire run strategy around. No more visual effects. Pure gameplay innovation where the AI is the engine.

- **NPC Ultimatum (Endgame Showdown Trigger)**: When the player is on their LAST available guess (one guess remaining before encounter failure), the NPC breaks protocol and delivers an Ultimatum instead of a normal clue. The PromptBuilder sends a special prompt: "The player is on their final guess. You have one last chance to either help them or toy with them. Deliver an ultimatum — a single, emotionally charged sentence that either contains a crucial hint disguised in your personality, or a taunt that might psyche them out. You choose." The Ultimatum replaces the normal clue flow — no clue request, just this one dramatic line. On the UI, the Ultimatum is displayed with a unique treatment: the entire screen dims except the clue text area, the CRT noise spikes to maximum then drops to dead silence (zero noise, zero scanlines — unnervingly clean), and the text types at half the NPC's normal speed. A countdown timer appears: `> FINAL ANSWER IN: 15s`. The player has 15 seconds to submit their final guess. The NPC's mood at this point (which has been shaped by the entire encounter) determines whether the Ultimatum leans helpful or hostile — an "encouraging" Guide at this moment gives a near-giveaway; a "menacing" Whisperer gives something that sounds helpful but might be misdirection. This creates the single most dramatic moment in any encounter — the instant where the AI's accumulated personality meets the player's desperation. Implementation: `UltimatumSystem` component. Subscribes to `GuessSubmittedEvent`. Tracks remaining guesses via `EncounterManager.GuessesRemaining`. When guesses == 1, intercepts the next clue cycle: instead of `RequestNextClue()`, builds a special ultimatum prompt via `PromptBuilder.BuildUltimatumPrompt()` (new method, ~20 lines) and sends it through `LLMManager`. On response, dims the screen (CanvasGroup alpha on a full-screen overlay), kills CRT effects temporarily (`CRTSettings.SetCleanMode(true)`), types the ultimatum text, starts 15-second countdown. On guess submit or timeout, restores normal CRT. ~130 lines across UltimatumSystem (~60), PromptBuilder (+20), LLMManager (no change — reuses existing generate flow), CRTSettings (+10 for clean mode), EncounterUI (+40 for dim overlay + countdown). | Checklist: ✅ Screen dim + clean CRT + slow type is immediate and dramatic ✅ Triggers on the player's final guess — the culmination of all their word-guessing ✅ The NPC's accumulated mood determines whether the ultimatum helps or hurts ✅ Special prompt + UI overlay + CRT toggle + countdown, ~1.5 days

- **Word Echo (Cross-Encounter Letter Persistence)**: When the player successfully solves a word, the FIRST LETTER of that word gets "imprinted" into the run's memory. In the NEXT encounter, if the new target word contains any imprinted letters, those letters start pre-revealed on the board with a distinctive visual mark (faint glow, "echo" shimmer animation on the tile). The PromptBuilder tells the NPC: "The player has echoed letters [X, Y, Z] from previous victories. These letters are already visible. Adjust your clue difficulty accordingly — you may need to be more cryptic since the player has a head start." The NPC's response to echoed letters creates a visible AI reaction: facing a board with 3 pre-revealed echo letters, the NPC might shift to "frustrated" mood immediately (Trickster), or "amused" mood (Guide — happy the player is doing well). The strategic depth: the player starts thinking about FUTURE encounters while solving the CURRENT one. "If I solve this word, the 'S' carries forward — and 'S' is common, so it'll probably help me next round." This creates a meta-strategy layer that rewards vocabulary knowledge (knowing which letters are common across words) and ties individual encounter performance to run-level progression. Echoed letters persist for the entire run and accumulate — by encounter 4-5, the player might have 5-6 echo letters giving them significant head starts. Implementation: `WordEchoSystem` component on RunManager. On `EncounterWonEvent`, extracts the first letter of the solved word, adds to `HashSet<char> _echoedLetters`. On encounter setup, `EncounterManager` reads the echo set and pre-reveals matching tiles via `BoardState.RevealEchoLetters(HashSet<char>)` (new method, ~10 lines). `TileBoardUI` marks echo-revealed tiles with a shimmer animation (DOTween loop on alpha/scale). PromptBuilder receives the echo letter set and appends context. ~85 lines across WordEchoSystem (~30), BoardState (+10), EncounterManager (+15), PromptBuilder (+10), TileBoardUI (+20). | Checklist: ✅ Echo letters appear pre-revealed when the encounter board loads ✅ Built from successfully guessed words — spelling performance IS the input ✅ NPC prompt adjusts to echo letters, AI mood shifts based on player's head start ✅ HashSet + pre-reveal + shimmer + prompt line, ~1 day

- **NPC Rival System (Persistent Antagonist Arc)**: At run start, one of the non-boss NPCs is randomly designated as the player's "Rival" for this run. The Rival appears in their normal encounter slot but with escalating behavior: in their first encounter, the Rival's prompt includes "You consider this player unworthy. Prove it with your clues." In their second encounter: "The player bested you once. You're angry. Your clues should be harder but your pride makes you occasionally slip." In their third encounter: "This is personal now. Give one genuinely good clue, then make every subsequent clue deliberately oblique." On the UI, the Rival's encounter has a unique visual frame: the CRT border tint shifts to a signature color (the Rival "claims" the screen), and their name displays with a `[RIVAL]` tag. Between encounters, the map node for the Rival's encounter has a special icon (crossed swords). The Rival's final encounter (if the player reaches it) offers double gold and a guaranteed Rare tome on victory. The emotional payoff: the player develops a personal relationship with one NPC across the run. "I beat the Trickster three times and he got progressively more unhinged" is a STORY. The Rival designation is shown on the map from the start, so the player anticipates and prepares. The Rival never appears as the boss NPC — they're a recurring mid-run antagonist. Implementation: `RivalSystem` component on RunManager. On run start, randomly selects a non-boss NPC archetype as `_rivalArchetype`. On encounter start, if current NPC matches rival, increments `_rivalEncounterCount` and injects escalating prompt context via PromptBuilder. `MapUI` marks rival nodes with a special icon (recolor existing node). `EncounterUI` shows `[RIVAL]` tag and applies border tint via `CRTSettings.SetBorderTint(Color)`. On rival encounter win, adds bonus gold via `RunManager` and queues a tome reward. ~110 lines across RivalSystem (~45), PromptBuilder (+20 for 3-tier escalating prompt), MapUI (+15 for node icon), EncounterUI (+15 for rival frame), RunManager (+15 for bonus rewards). | Checklist: ✅ [RIVAL] tag and CRT border tint visible immediately on encounter start ✅ Rival encounters are word-guessing encounters with escalating difficulty ✅ Rival's prompt escalates across encounters — AI personality deepens per meeting ✅ Counter + conditional prompts + UI tags + reward hooks, ~1.5 days

### Surviving ideas (cumulative)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- NPC Typing Hesitation (Live Generation Tells)
- NPC Wager Reaction (Pre-Encounter Mood Seed)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- NPC Vocabulary Echo (Word Pattern Feedback)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)
- NPC Endgame Monologue (Victory/Defeat Personality Cascade)
- NPC Word Veto (AI Refuses a Word)
- Clue Bidding (Gold-for-Quality Trade)
- NPC Grudge Escalation (Cumulative Run Tension)
- NPC Ultimatum (Endgame Showdown Trigger)
- Word Echo (Cross-Encounter Letter Persistence)
- NPC Rival System (Persistent Antagonist Arc)

### Killed this pass
- (none — all three pass checklist, and these are the final generator ideas)

### Resume token
LAST_PASS=19 | BEST_IDEAS=27 | NEXT_ROLE=Critic

---

## Pass 20 | 2026-03-04 23:00 | Critic (FINAL PASS)
### Analysis

27 ideas. Final pass. I need to be ruthless. The question isn't "is this idea good?" — it's "does this idea DEFINE the game?" I'm cutting everything that doesn't clear that bar.

**Kill #1: NPC Vocabulary Echo (Word Pattern Feedback).** This fires on every 2nd wrong phrase guess and shows a curated template selected by Levenshtein distance + mood. Problems: (1) It triggers on PHRASE misses, which happen 0-3 times per encounter — most encounters end before a 2nd phrase miss. The idea has almost zero trigger frequency. (2) The Levenshtein distance classification (warm/cold) is local computation, not AI-driven. The mood component just picks which STRING BANK to select from. (3) Writing 120 template strings (30 per NPC x 4 NPCs) is a massive content authoring burden for a feature that fires once or twice per run. The effort-to-impact ratio is terrible. (4) It occupies the same UI slot as the now-killed Mood Shift Narration — and with that dead, this idea loses its UI anchor. NPC Endgame Monologue does the "NPC personality text" job better at a more impactful moment.

**Kill #2: NPC Wager Reaction (Pre-Encounter Mood Seed).** This depends on the Confidence Bet/Gambit system (from Pillar 2) existing first — it's a reaction layer on top of a system that hasn't been built. The idea itself is thin: inject bet context into the prompt, shake the NPC name text. The prompt injection is ~10 lines in PromptBuilder. The name shake is a single DOTween call. This isn't an "idea" — it's a polish detail that naturally emerges when you implement betting. It doesn't deserve a separate slot on the surviving list because it's not a standalone mechanic; it's a line of code inside the bet system.

**Kill #3: NPC Typing Hesitation (Live Generation Tells).** This extends the generation theater (from Pillar 1) by making the theater's typing speed vary by last mood. But the generation theater itself is already a Pillar 1 item (section 1.3) that types NPC-flavored terminal lines. Making those lines type faster when the NPC was amused vs. slower when frustrated is a tuning parameter, not a separate mechanic. The idea's description even says "directly enhances the generation theater rather than adding a new system." If it's not a new system, it's not a separate idea — it's a refinement of an existing one. Fold it into the generation theater implementation as a mood-speed multiplier and move on.

**No replacements.** We are pruning to a final roster. 24 ideas is the ceiling.

### Surviving ideas (FINAL — 24 ideas)
- Mood Pulse Tiles
- NPC Patience Meter
- Reactive Clue Intensity (NPC Tells)
- NPC Interrupt (Taunt on Repeat)
- Mood Glitch Transition (CRT Mood Cuts)
- Tile Corruption on NPC Frustration
- NPC Grudge Memory (Cross-Encounter Callback)
- Mood Bargain (Mid-Encounter Deal)
- NPC Adaptive Difficulty (Mercy/Cruelty Shift)
- Spell Streak (Combo Momentum)
- NPC Bluff Clue (Deception Mechanic)
- Letter Sacrifice (Strategic Tile Trade)
- Mood Lock (Emotional Pinning via Streaks)
- NPC Trap Tile (Decoy Board Hazard)
- NPC Handoff (Multi-NPC Encounter)
- Vowel Gambit (Risk-Reward Letter Category)
- Clue Decay (Timed Information Erosion)
- NPC Endgame Monologue (Victory/Defeat Personality Cascade)
- NPC Word Veto (AI Refuses a Word)
- Clue Bidding (Gold-for-Quality Trade)
- NPC Grudge Escalation (Cumulative Run Tension)
- NPC Ultimatum (Endgame Showdown Trigger)
- Word Echo (Cross-Encounter Letter Persistence)
- NPC Rival System (Persistent Antagonist Arc)

### Killed this pass
- NPC Vocabulary Echo (Word Pattern Feedback) — triggers at most once or twice per run (2nd phrase miss is rare), Levenshtein distance is local computation not AI-driven, 120 template strings is massive content burden for minimal trigger frequency
- NPC Wager Reaction (Pre-Encounter Mood Seed) — depends on unbuilt Confidence Bet system, amounts to ~10 lines of prompt injection + one DOTween call, not a standalone mechanic but a polish detail within the bet system
- NPC Typing Hesitation (Live Generation Tells) — self-described as "enhances the generation theater rather than adding a new system," is a mood-speed multiplier on an existing Pillar 1 feature, not a separate idea

### Resume token
LAST_PASS=20 | BEST_IDEAS=24 | NEXT_ROLE=DONE

---

## SUMMARY

### Top 5 Ideas by Impact and Checklist Score

These are the five ideas that, if shipped together, would most transform Spellwright from "a word game with invisible AI" into "a game where you're dueling a living opponent."

---

**1. NPC Adaptive Difficulty (Mercy/Cruelty Shift)**

The single most important idea in the entire list. The AI's mood — which the player's performance directly influences — becomes the difficulty tuning knob. A frustrated NPC (player is struggling) triggers mercy mode: +1 letter revealed per clue, prompt instructs easier clues. An amused NPC (player is dominating) triggers cruelty mode: 0 reveals, prompt instructs oblique clues. The player FEELS the AI pushing back when they're winning and yielding when they're losing, creating natural rubber-banding that makes the AI feel intelligent and responsive.

**Implementation:** Create `AdaptiveDifficultyMod.cs` in `Scripts/Encounter/` (~40 lines). Subscribe to `ClueReceivedEvent` on the EventBus. Read `Clue.Mood` and classify into Mercy/Normal/Cruel via a switch statement. Store the shift as a field. In `EncounterManager.cs`, before calling `RequestNextClue()`, read the shift from `AdaptiveDifficultyMod` and temporarily modify `GameConfigSO.lettersRevealedPerClue` (restore after the clue cycle). In `PromptBuilder.cs`, add ~15 lines to append a difficulty hint string to the user message when mercy/cruelty is active. In `EncounterUI.cs`, add a small TextMeshPro label (`[SIGNAL: BOOSTED]` / `[SIGNAL: DEGRADED]`) positioned near the clue area, colored green/red, toggled by the shift state. Total: ~90 lines across 4 files.

---

**2. NPC Ultimatum (Endgame Showdown Trigger)**

This creates the single most memorable moment in every encounter. When the player reaches their final guess, the entire screen transforms: dims, CRT goes clean (zero noise/scanlines), and the NPC delivers one dramatic, mood-shaped line. The AI's accumulated personality across the encounter crystallizes into a single sentence that either saves or damns the player. This is the moment players will screenshot and share.

**Implementation:** Create `UltimatumSystem.cs` in `Scripts/Encounter/` (~60 lines). Subscribe to `GuessSubmittedEvent`. Track `EncounterManager.GuessesRemaining`. When remaining == 1, set a flag to intercept the next clue cycle. In `PromptBuilder.cs`, add `BuildUltimatumPrompt(NPCPromptData npc, string mood, string word)` method (~20 lines) that constructs a short, high-drama prompt. The LLM call goes through the existing `LLMManager.GenerateClue()` path with the special prompt. In `CRTSettings.cs`, add `SetCleanMode(bool)` (~10 lines) that zeroes out noise/scanlines/aberration and caches previous values for restoration. In `EncounterUI.cs`, add a full-screen dim overlay (CanvasGroup with black Image, alpha 0.6) and a countdown timer TextMeshPro (`> FINAL ANSWER IN: 15s`), activated by `UltimatumSystem` via an event. Total: ~130 lines across 4 files.

---

**3. NPC Rival System (Persistent Antagonist Arc)**

This gives every run a personal narrative. One NPC is your Rival from the start, and their prompt escalates across encounters — from dismissive to furious to desperate. The player develops a relationship with a specific AI personality across 3+ encounters, creating stories ("My Trickster rival went completely unhinged by encounter 3"). The escalating prompt injection means the AI genuinely behaves differently each time.

**Implementation:** Create `RivalSystem.cs` in `Scripts/Run/` (~45 lines). On `RunStartedEvent`, randomly select a non-boss NPC archetype from `GameManager.regularNPCs` and store as `_rivalArchetype`. Track `_rivalEncounterCount`. On `EncounterStartedEvent`, check if current NPC matches rival; if so, increment count. In `PromptBuilder.cs`, add a `rivalTier` parameter to `BuildSystemPrompt()` (+20 lines) with 3 tiers of escalating personality injection. In `MapUI.cs`, mark rival nodes by checking the node's NPC against `RivalSystem.RivalArchetype` and applying a tint/icon to the node button (+15 lines). In `EncounterUI.cs`, show `[RIVAL]` tag next to NPC name and call `CRTSettings.Instance.SetBorderTint(rivalColor)` on encounter start (+15 lines). In `RunManager.cs`, on rival encounter win, add bonus gold via existing `AddGold()` and queue a tome reward (+15 lines). Total: ~110 lines across 5 files.

---

**4. Mood Bargain (Mid-Encounter Deal)**

This is the idea that turns mood from a passive signal into a strategic resource. When the AI's mood shifts to frustrated or amused, the NPC OFFERS the player a deal — trade a guess for a free vowel, or accept double risk for double reward. The player must decide in 8 seconds. This forces the player to read the AI's emotional state and make real-time strategic decisions based on it. Combined with other mood systems, the player starts trying to MANIPULATE the NPC's mood to trigger favorable bargains.

**Implementation:** Create `MoodBargainSystem.cs` in `Scripts/Encounter/` (~50 lines). Subscribe to `ClueReceivedEvent`. Compare `Clue.Mood` to stored `_lastMood`. If mood changed to frustrated/amused, select a deal from a `Dictionary<(string archetype, string mood), BargainTemplate>` (~12 entries). `BargainTemplate` is a simple struct: description string, effect enum (RevealVowel, DoubleRisk, SkipGuess), cost description. In `EncounterUI.cs`, add a bargain panel (TextMeshPro + Button + countdown fill Image) that shows on `BargainOfferedEvent` and hides after 8 seconds or on click (+40 lines). On accept click, publish `BargainAcceptedEvent` with the effect type. `EncounterManager.cs` subscribes and applies the effect: for RevealVowel, call `BoardState.RevealRandomVowel()` (new 10-line method); for DoubleRisk, set `_hpMultiplier = 2` for next wrong guess; for SkipGuess, decrement `_guessesRemaining` (+30 lines). New events in `EventBus`: `BargainOfferedEvent`, `BargainAcceptedEvent` (~15 lines). Total: ~130 lines across 4 files + event definitions.

---

**5. Letter Sacrifice (Strategic Tile Trade)**

The most mechanically novel idea. The player voluntarily RE-HIDES a correctly revealed letter to force the NPC to give a better clue. This inverts the core loop — you're giving up information you earned to demand better AI assistance. The NPC's personality shapes the sacrifice response (Guide gives a genuine upgrade, Trickster might cheat you). This creates agonizing decisions: "I have 7 of 10 letters. If I sacrifice the 'E', I drop to 6 but get a clue that might let me solve immediately."

**Implementation:** Create `LetterSacrificeSystem.cs` in `Scripts/Encounter/` (~40 lines). Add a "Sacrifice Mode" toggle button to `EncounterUI` (styled as `[SACRIFICE: OFF]` / `[SACRIFICE: ON]`). When active, tile clicks on REVEALED tiles trigger sacrifice instead of normal behavior. In `BoardState.cs`, add `RehideTile(int index)` method (~8 lines) that sets `TileState` back to `Hidden` and clears the revealed letter. Publish `LetterSacrificedEvent`. `EncounterManager.cs` subscribes to `LetterSacrificedEvent` and calls `RequestNextClue()` with a `sacrifice: true` flag (+25 lines). `PromptBuilder.cs` appends sacrifice context to the prompt when the flag is set (+15 lines). `TileBoardUI.cs` plays a shatter animation on the re-hidden tile: DOTween scale to zero over 0.3s, then rebuild as hidden tile (+20 lines). Sacrifice is limited to once per encounter via a `_sacrificeUsed` flag. Total: ~120 lines across 5 files.

---

### Final Tally

**Surviving ideas: 24**

Ideas killed across passes 1-20: Clue Echo Trail, Vowel Scarcity Radar, AI Confidence Preview, Board Heartbeat, NPC Signature Sound on Guess, Tile Loyalty Marks, Letter Heat Map, NPC Stutter on Near-Miss, Ghost Guess, NPC Difficulty Tell, Mood Shift Narration, NPC Vocabulary Echo, NPC Wager Reaction, NPC Typing Hesitation.

### Design North Star

**"Every guess is a conversation with an opponent who remembers, reacts, and retaliates — the AI is not behind the curtain, it IS the curtain."**
