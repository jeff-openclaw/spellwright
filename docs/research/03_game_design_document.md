# Spellwright — Game Design Document (First Draft)

*Version 0.1 — 2026-02-28*
*Reference: [02_game_design.md](./02_game_design.md) for underlying research*

---

## 1. Game Pillars

### Pillar 1: Every Word Tells a Story
Encounters aren't puzzles — they're conversations. Each NPC has personality, motivation, and a unique way of hiding their word. The player isn't just solving; they're *engaging with a character*.

### Pillar 2: Build Your Brain
Tomes (modifiers) don't make the player stronger in a generic sense — they change *how the player thinks*. A Tome that reveals vowels changes your deduction strategy. A Tome that doubles reward for 8+ letter words changes what risks you take. The build *is* the playstyle.

### Pillar 3: No Two Runs Alike
LLM-generated clues + random Tome combinations + NPC archetype variety = every run feels genuinely different. Not "different arrangement of the same pieces" but "I've never seen this clue before."

### Pillar 4: Readable Depth
Easy to understand in 30 seconds ("guess the word from clues"), deep enough for 100 hours of mastery. Complexity comes from Tome synergies and NPC exploitation, not from obscure rules.

---

## 2. Run Structure

### 2.1 Overview

A run consists of **3 Acts**, each with **3 Floors** + **1 Boss**. Total: 12 encounters per run (9 regular + 3 bosses). Expected run duration: **25-40 minutes**.

```
ACT 1: The Foothills (Floors 1-3 + Boss)
  → Forgiving. Learn Tomes. Build foundation.
  
ACT 2: The Labyrinth (Floors 4-6 + Boss)  
  → Challenging. Synergies matter. Strategy commits.
  
ACT 3: The Spire (Floors 7-9 + Final Boss)
  → Punishing. Only strong builds survive.
```

### 2.2 Map Structure

Each Act presents a **branching path** (inspired by Slay the Spire):

```
Floor 1:  [Encounter] ─── [Encounter]
              │    ╲        ╱    │
Floor 2:  [Encounter] ─ [Shop] ─ [Encounter]
              │    ╲        ╱    │
Floor 3:  [Encounter] ─── [Rest/Event]
              │              │
Boss:     [═══ BOSS ENCOUNTER ═══]
```

**Node Types:**
- **Encounter** (⚔️): Standard word-guessing encounter with NPC
- **Shop** (🏪): Buy/sell Tomes, buy healing, reroll offerings
- **Rest** (🏕️): Heal HP or upgrade a Tome
- **Event** (❓): Random event — could be beneficial, neutral, or a trap
- **Boss** (💀): Harder encounter with unique rule modification

### 2.3 Between Encounters

After each encounter:
- **Rewards screen**: Gold earned (base + bonuses), Tome choice (pick 1 of 3)
- **Interest**: Earn 1 gold per 5 gold held (max +5 interest, cap at 25 gold banked)
- **Map choice**: Pick next node on branching path

---

## 3. Encounter Flow

### 3.1 Standard Encounter (Step by Step)

```
1. ENTRANCE
   - NPC appears with brief intro dialogue (1-2 sentences)
   - NPC archetype and difficulty indicator shown
   - Word length shown as blank tiles: _ _ _ _ _ _
   - Category hint shown (e.g., "Animals", "Emotions", "Tools")

2. FIRST CLUE
   - NPC delivers opening clue in their personality style
   - Player reads clue, considers possibilities
   
3. GUESS PHASE (repeats until solved or HP depleted)
   a. Player types a guess and submits
   b. IF CORRECT:
      → Victory! Score calculated. Rewards granted.
      → Earlier correct = bigger reward (guess 1 = 3x, guess 2 = 2x, guess 3+ = 1x)
   c. IF INCORRECT:
      → Player loses HP (base: 1 HP per wrong guess)
      → Guess feedback: "Too short/long", "Wrong category", or Tome-dependent hints
      → NPC delivers next clue (more specific than previous)
      → Active Tomes may trigger effects (reveal a letter, reduce cost, etc.)

4. RESOLUTION
   - If solved: Gold + score + possible Tome drop
   - If HP reaches 0: Run ends (death screen, stats, unlocks)

5. REWARD SELECTION
   - Choose 1 of 3 offered Tomes (or skip for gold bonus)
   - Proceed to map
```

### 3.2 Boss Encounter Modifications

Each boss adds a **rule twist** that persists for the entire encounter:

| Boss | Rule Twist | Act |
|------|-----------|-----|
| **The Whisperer** | Clues are only 3 words long (forces inference from minimal info) | 1 |
| **The Scrambler** | Word's blank tiles are shuffled (you don't know the length ordering) | 2 |
| **The Eraser** | One random Tome is disabled for the encounter | 2 |
| **The Polyglot** | Clues contain words from other languages (player must infer meaning) | 3 |
| **The Void** | No category hint. No word length shown. Clues only. | 3 (Final) |

### 3.3 Guess Feedback Model

Spellwright uses a **clue-based** feedback model (not Wordle-style letter positions):

- **Wrong guess, wrong length**: "The word has [N] letters, not [M]."
- **Wrong guess, right length**: NPC gives a new, more specific clue
- **Wrong guess, semantically close**: NPC reacts ("Warmer..." / "You're circling it...")
- **Tome-enhanced feedback**: Depending on equipped Tomes, may also reveal: first letter, vowel count, rhyming word, etc.

**Semantic closeness** is evaluated via word embeddings (pre-computed, not LLM). Words within cosine distance threshold trigger "warm" responses.

---

## 4. Tome Modifiers

### 4.1 Design Principles

- **Slot limit**: 5 active Tome slots (expandable to 7 via rare Tomes)
- **Rarity tiers**: Common (white), Uncommon (green), Rare (blue), Legendary (gold)
- **Categories**: Insight (information), Economy (gold), Defense (HP), Offense (scoring), Meta (modify other Tomes)

### 4.2 Tome Catalog (20 Examples)

#### Insight Tomes (Information Advantage)

| # | Tome | Rarity | Effect | Synergy Notes |
|---|------|--------|--------|---------------|
| 1 | **Vowel Lens** | Common | Reveals all vowels in the hidden word after first wrong guess | Synergizes with short-word strategies; less useful for long words |
| 2 | **First Light** | Common | The first letter of the word is always revealed | Strong baseline Tome; pairs with Alphabetist |
| 3 | **Echo Chamber** | Uncommon | After a wrong guess, learn if any of its letters appear in the answer | Wordle-style feedback on top of clue system; very powerful |
| 4 | **Rhyme Scheme** | Uncommon | NPC's first clue must include a word that rhymes with the answer | Extremely powerful for short words; NPC prompt includes rhyming constraint |
| 5 | **Etymology Scroll** | Rare | Word's language of origin is shown (Latin, Greek, Germanic, etc.) | Niche but powerful for vocabulary-strong players |

#### Economy Tomes (Gold Generation)

| # | Tome | Rarity | Effect | Synergy Notes |
|---|------|--------|--------|---------------|
| 6 | **Miser's Purse** | Common | +1 max interest cap (from +5 to +6) | Stacks with other economy Tomes; rewards saving |
| 7 | **Word Tax** | Common | Earn +2 gold for solving words with 7+ letters | Incentivizes taking on harder/longer words |
| 8 | **Haggler's Tongue** | Uncommon | Shop prices reduced by 15% | Passive value; better in long runs |
| 9 | **Bounty Board** | Rare | Earn +10 gold for first-try solves | High skill reward; synergizes with Insight Tomes |
| 10 | **Golden Quill** | Legendary | Earn 1 gold per letter in correctly guessed words | Massive with long words; build-defining |

#### Defense Tomes (Survival)

| # | Tome | Rarity | Effect | Synergy Notes |
|---|------|--------|--------|---------------|
| 11 | **Thick Skin** | Common | +10 max HP | Simple but effective; enables aggressive guessing |
| 12 | **Second Wind** | Uncommon | Once per encounter, a wrong guess costs 0 HP | Safety net; allows one free "shot in the dark" |
| 13 | **Leech Letter** | Rare | Heal 2 HP on correct solve | Sustain over long runs; better in Act 2-3 |
| 14 | **Mirror Ward** | Rare | Boss rule twists affect the NPC too (boss gives shorter clues but also reveals more) | Anti-boss tech; situationally very strong |

#### Offense Tomes (Scoring / Multipliers)

| # | Tome | Rarity | Effect | Synergy Notes |
|---|------|--------|--------|---------------|
| 15 | **Speed Reader** | Common | +50% score for solving within 30 seconds | Time pressure for bonus; pairs with Insight Tomes |
| 16 | **Polymath** | Uncommon | Score multiplier +0.5x for each unique word category solved this run | Rewards variety; anti-specialization |
| 17 | **Perfectionist** | Rare | 3x score multiplier for first-try solves (replaces normal 3x with 5x) | High risk, high reward; build-around |
| 18 | **Chain Lightning** | Legendary | Each consecutive correct solve without a wrong guess adds +1x multiplier (resets on wrong guess) | Streak mechanic; incredibly powerful but fragile |

#### Meta Tomes (Modify Rules / Other Tomes)

| # | Tome | Rarity | Effect | Synergy Notes |
|---|------|--------|--------|---------------|
| 19 | **Tome of Tomes** | Rare | +1 Tome slot (max 1 copy) | Enables bigger builds; always valuable |
| 20 | **Chaos Grimoire** | Legendary | At the start of each encounter, one random Tome's effect is doubled, one is disabled | High variance; exciting for experienced players |

### 4.3 Synergy Examples

**"The Scholar" Build** (Insight-heavy):
- Vowel Lens + First Light + Echo Chamber + Rhyme Scheme + Perfectionist
- Strategy: Stack information advantages to consistently first-try solve. Perfectionist converts information into massive score multipliers.

**"The Miser" Build** (Economy-focused):
- Miser's Purse + Word Tax + Bounty Board + Golden Quill + Haggler's Tongue
- Strategy: Accumulate gold rapidly. Buy every shop upgrade. Out-scale difficulty through sheer resource advantage.

**"The Daredevil" Build** (High-risk offense):
- Speed Reader + Chain Lightning + Second Wind + Thick Skin + Chaos Grimoire
- Strategy: Guess fast, maintain streaks, use defensive Tomes as safety net for the inevitable miss.

---

## 5. NPC Archetypes

| Archetype | Personality | Clue Style | Encounter Feel | Difficulty Modifier |
|-----------|------------|------------|----------------|-------------------|
| **Riddlemaster** | Ancient, formal, amused | Riddles and metaphors | Cerebral, literary | Harder (oblique clues) |
| **Trickster Merchant** | Sly, funny, sales-pitch | "Product descriptions" | Light, comedic | Medium (playful misdirection) |
| **Drunken Sage** | Warm, rambling, wise | Tangential wisdom | Charming, unpredictable | Medium (useful info buried in noise) |
| **Silent Librarian** | Cold, precise, minimal | Dictionary definitions | Clinical, efficient | Easier (direct information, less of it) |
| **Rival Adventurer** | Smug, competitive, tsundere | Taunts and dares | Energetic, personal | Harder (clues are backhanded) |

Each NPC archetype has a **full system prompt** (see Research Report §4.4). NPCs are randomly assigned to encounters but weighted by Act difficulty:
- Act 1: Librarian and Merchant more common (friendlier clues)
- Act 2: Mixed distribution
- Act 3: Riddlemaster and Rival more common (harder clues)

---

## 6. Difficulty Curve

### 6.1 Difficulty Axes

| Axis | Act 1 | Act 2 | Act 3 |
|------|-------|-------|-------|
| Word length | 4-5 letters | 5-7 letters | 6-9 letters |
| Word commonality | Top 3000 words | Top 8000 words | Top 15000 words |
| Clue obscurity | Direct associations | Lateral associations | Abstract/metaphorical |
| HP cost per wrong guess | 1 | 2 | 3 |
| Guesses before "warm" hint | 1 wrong guess | 2 wrong guesses | 3 wrong guesses |
| Score target to "win" floor | 100 | 300 | 800 |
| NPC archetype weighting | Friendly | Mixed | Hostile |

### 6.2 Player HP

- **Starting HP**: 30
- **Max HP**: 40 (expandable via Tomes)
- **Healing**: Rest nodes (+8 HP), Leech Letter Tome, shop healing items (5 gold = 5 HP)
- **HP loss**: Wrong guesses (1-3 per Act), traps in Events, boss abilities

### 6.3 Difficulty Modifiers (Post-Win Unlocks)

After completing a run, unlock harder modes (like Balatro's stakes):

| Modifier | Effect |
|----------|--------|
| **Apprentice** (default) | Standard difficulty |
| **Journeyman** | Start with 25 HP. Words are 1 letter longer on average. |
| **Adept** | Shops cost 25% more. One fewer Tome choice per reward. |
| **Master** | Boss encounters have 2 rule twists instead of 1. |
| **Grandmaster** | Clues are 50% shorter. No category hints. Start with 20 HP. |

---

## 7. Scoring System

```
Base Score = Word Length × 10
Guess Bonus = {First try: ×3, Second try: ×2, Third try: ×1.5, 4+: ×1}
Tome Multipliers = (applied multiplicatively from Offense Tomes)
Time Bonus = (if Speed Reader equipped, +50% for <30s)

Final Score = Base Score × Guess Bonus × Tome Multipliers × Time Bonus

Gold Earned = floor(Final Score / 10) + base encounter gold (5-15)
```

---

## 8. Shop

### 8.1 Shop Contents

Each shop visit offers:
- **3 Tomes** (random rarity, weighted by Act: more rares in later Acts)
- **1 Healing Item** (5 gold = 5 HP)
- **1 Tome Removal** (remove a Tome for 5 gold — useful for build pivots)
- **Reroll** (5 gold to refresh Tome offerings)

### 8.2 Tome Prices

| Rarity | Price |
|--------|-------|
| Common | 5-8 gold |
| Uncommon | 10-15 gold |
| Rare | 18-25 gold |
| Legendary | 30-40 gold |

### 8.3 Sell-Back

Players can sell equipped Tomes at any time for 50% of purchase price (or 3 gold if found, not bought).

---

## 9. Events

Random events on the map. Examples:

| Event | Description |
|-------|------------|
| **The Wandering Scribe** | Offers to upgrade one Tome to the next rarity tier for free — but picks randomly |
| **Fork in the Road** | Choose: +10 gold OR +5 HP OR a random Common Tome |
| **Cursed Library** | Gain a powerful Rare Tome, but lose 5 max HP permanently |
| **The Word Merchant** | Peek at the next encounter's word category (information advantage) |
| **Déjà Vu** | Replay the previous encounter for double rewards (but same word — do you remember it?) |

---

## 10. MVP Scope vs. Full Game Scope

### 10.1 MVP (Target: 3-month development)

**INCLUDED in MVP:**

| Feature | Scope |
|---------|-------|
| Core encounter loop | Fully functional: NPC clue → player guess → feedback → reward |
| NPC system | 3 archetypes (Riddlemaster, Merchant, Librarian) with LLM integration |
| Tome system | 10 Tomes (2 per category), 5 slots, Common + Uncommon only |
| Run structure | 1 Act (3 floors + 1 boss), linear path (no branching) |
| Shop | Basic: 2 Tomes + healing, no reroll |
| Difficulty | Single difficulty level, fixed word list (500 words) |
| UI | Functional text-based UI, minimal animations |
| Word validation | Server-side string matching, basic semantic closeness |
| Scoring | Base score + guess bonus, no multiplier stacking |
| Platform | Web (browser-based), single player |

**CUT from MVP:**

| Feature | Reason for Cut |
|---------|---------------|
| Acts 2-3, branching map | Scope — validate Act 1 first |
| 2 NPC archetypes (Sage, Rival) | Can add after core loop validated |
| 10 additional Tomes, Rare/Legendary tiers | Balance requires playtesting data |
| Events, Rest nodes | Non-essential for core loop validation |
| Difficulty modifiers (post-win) | No replaying until core is fun |
| Interest/economy system | Simplify to flat gold rewards in MVP |
| Tome synergy combos | Needs larger Tome pool to matter |
| Sound/music | Polish phase |
| Tome upgrades at rest nodes | Feature creep |
| Meta-progression (permanent unlocks) | Post-MVP retention feature |
| Pre-cached clue system | Optimize after validating LLM approach |
| Mobile/native clients | Web first |

### 10.2 Full Game (Target: 12-month development)

Everything in MVP plus:
- 3 full Acts with branching map
- 5 NPC archetypes with 2-3 variants each
- 30+ Tomes across 4 rarity tiers
- Events, Rest nodes, Tome upgrades
- 5 difficulty modifiers
- Interest/economy system
- Meta-progression: unlock new Tomes, NPC variants, starting bonuses
- Daily challenge mode (fixed seed, leaderboard)
- Sound design + music
- Pre-cached clue optimization for cost reduction
- Accessibility: colorblind mode, font size options, screen reader support
- Analytics: track word difficulty, Tome pick rates, NPC satisfaction for balance tuning
- 2000+ word vocabulary with difficulty ratings

### 10.3 MVP Success Criteria

The MVP validates these hypotheses:
1. **Core loop is fun**: Players enjoy the clue → guess → reward cycle (measured: session length > 15 min, replay rate > 2 runs)
2. **LLM NPCs add value**: Players prefer LLM clues over static clues (A/B test)
3. **Tomes change behavior**: Players adjust guessing strategy based on equipped Tomes (measured: guess patterns differ across builds)
4. **Technical feasibility**: LLM latency < 2 seconds, cost per run < $0.05, answer leakage rate < 1%

---

## 11. Technical Architecture (Brief)

```
┌──────────────┐     ┌─────────────────┐     ┌──────────────┐
│   Browser    │────▶│   Game Server   │────▶│   LLM API    │
│   Client     │◀────│   (Node/Python) │◀────│  (Claude/GPT) │
└──────────────┘     └────────┬────────┘     └──────────────┘
                              │
                     ┌────────▼────────┐
                     │    Word DB      │
                     │  - word list    │
                     │  - categories   │
                     │  - difficulty   │
                     │  - embeddings   │
                     └─────────────────┘
```

**Key decisions:**
- Hidden word NEVER sent to LLM (see Research Report §4.5)
- Guess validation is server-side string comparison
- Semantic closeness uses pre-computed word embeddings (not LLM)
- NPC responses validated before sending to client
- Structured JSON responses from LLM, parsed server-side, flavor text rendered client-side

---

## 12. Open Questions

1. **Word list curation**: How to balance "interesting words" vs. "fair words"? Need playtesting.
2. **LLM model choice**: Claude vs. GPT-4 vs. smaller fine-tuned model? Cost vs. quality tradeoff.
3. **Multiplayer potential**: Could a "competitive" mode work (two players, same word, race to solve)?
4. **Monetization**: Premium Tome cosmetics? Season pass with new NPC archetypes? Or just paid game?
5. **Accessibility**: How to handle players with limited English vocabulary? Difficulty settings? Language options?
6. **Anti-cheat**: Players could paste clues into external LLMs to solve. Does this matter for a single-player game?

---

*This GDD is a living document. Update after each playtest cycle.*
