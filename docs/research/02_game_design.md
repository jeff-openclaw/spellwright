# Spellwright — Game Design Research Report

*Date: 2026-02-28*

---

## 1. Competitor Gap Analysis

### 1.1 Glyphica: Typing Roguelike

- **Core Mechanic**: Type words to deal damage in a roguelike dungeon crawler. Words appear as enemies; you type them to attack. Modifiers alter typing rules (e.g., reversed words, timed bursts).
- **What Players Love**: Satisfying "flow state" from fast typing; clever modifier stacking; polished pixel art; surprisingly deep builds.
- **What Players Hate**: Repetitive word lists; difficulty spikes feel arbitrary; limited replayability once you've seen all modifiers; typing speed as the primary skill filter excludes casual players.
- **Gap for Spellwright**: Glyphica is about *typing speed*, not *word knowledge or deduction*. There's no puzzle element — you see the word and type it. Spellwright's guessing mechanic adds cognitive challenge independent of typing speed.

### 1.2 Wordlike

- **Core Mechanic**: Wordle-meets-roguelike. Solve a series of Wordle-style puzzles with roguelike progression. Each puzzle is an "encounter"; wrong guesses cost HP; modifiers change letter feedback rules.
- **What Players Love**: Familiar Wordle mechanic with added stakes; clean UI; satisfying "aha" moments; good difficulty curve.
- **What Players Hate**: Formulaic — every encounter is the same 5-letter grid; limited modifier variety; no narrative framing; gets stale after ~5 hours; word list is static and eventually memorizable.
- **Gap for Spellwright**: Wordlike uses a fixed Wordle grid. Spellwright's LLM-generated clues mean every encounter is narratively unique. Variable word length, thematic clues, and NPC personality add dimensions Wordlike completely lacks.

### 1.3 Tomb of the Bloodletter

- **Core Mechanic**: Scrabble-style word formation on a grid, combined with dungeon crawling. Place letter tiles to spell words that deal damage. Tile placement matters spatially.
- **What Players Love**: Deep strategic tile placement; "big brain" moments from long words on multiplier tiles; dark gothic atmosphere; satisfying power curve.
- **What Players Hate**: Steep learning curve; sometimes you just don't have the letters; RNG on tile draws can feel punishing; pacing can drag in longer runs.
- **Gap for Spellwright**: Tomb is about *word formation* (you produce words from available letters). Spellwright is about *word deduction* (you figure out what word the NPC is thinking of). Fundamentally different cognitive skill — production vs. recognition/inference.

### 1.4 Beyond Words

- **Core Mechanic**: RPG where combat involves forming words from letter tiles. Party-based system with different character abilities that modify available letters or word effects.
- **What Players Love**: RPG depth layered onto word gameplay; party synergies; narrative progression; accessible difficulty.
- **What Players Hate**: Combat gets repetitive; word-forming under pressure is stressful rather than fun for some; AI party members are simplistic; balancing issues with certain letter distributions.
- **Gap for Spellwright**: Beyond Words is a traditional RPG with word-forming bolted on. Spellwright inverts the dynamic — the NPC is the one with language mastery, and you're the one trying to decode it. The LLM creates a *conversation* rather than a *tile puzzle*.

### 1.5 OMG Words (formerly Wordox)

- **Core Mechanic**: Competitive multiplayer word game with roguelike-inspired progression systems. Form words on a shared board; steal opponents' letters; power-ups modify scoring.
- **What Players Love**: Competitive tension; steal mechanic adds drama; quick matches; social element.
- **What Players Hate**: Pay-to-win power-ups; matchmaking issues; mobile-first design feels shallow on PC; limited single-player content.
- **Gap for Spellwright**: OMG Words is multiplayer-competitive and word-formation. Spellwright is single-player, narrative-driven, and deduction-based. No overlap in core experience.

### 1.6 The Spellwright Differentiator

**What NONE of these competitors do:**

| Feature | Glyphica | Wordlike | Tomb | Beyond Words | OMG Words | **Spellwright** |
|---------|----------|----------|------|--------------|-----------|-----------------|
| LLM-generated clues | ✗ | ✗ | ✗ | ✗ | ✗ | **✓** |
| Guessing (deduction) vs forming | ✗ | Partial | ✗ | ✗ | ✗ | **✓** |
| NPC personality/dialogue | ✗ | ✗ | ✗ | Minimal | ✗ | **✓** |
| Infinite clue variety | ✗ | ✗ | ✗ | ✗ | ✗ | **✓** |
| Modifier stacking (Balatro-style) | Some | Some | ✗ | Some | ✗ | **✓** |
| Narrative framing per encounter | ✗ | ✗ | Light | Yes | ✗ | **✓** |

**Key insight**: The entire word-roguelike space is built on *word formation* (Scrabble/typing mechanics). Spellwright occupies the completely uncontested *word deduction* space, enhanced by LLM-driven narrative framing. This is the core competitive moat.

---

## 2. Word-Guessing Mechanics Analysis

### 2.1 Wordle

- **System**: 6 guesses to find a 5-letter word. Color-coded feedback: green (right letter, right position), yellow (right letter, wrong position), gray (letter not in word).
- **Tension Drivers**: Limited guesses create escalating pressure. Binary feedback (no partial credit) forces commitment. The "narrowing" feeling — going from 13,000 possibilities to 50 to 3 — is deeply satisfying.
- **Difficulty Levers**: Word length (fixed at 5); guess count (fixed at 6); word commonality (curated list avoids obscurity).
- **Satisfaction Model**: "Eureka" moment when the answer clicks. Social sharing (the colored grid) extends satisfaction beyond the game.
- **Lessons for Spellwright**: The narrowing funnel is the core joy. Spellwright should replicate this — each clue/guess should visibly narrow the possibility space. But Spellwright can go further: variable word length, thematic clues (not just letter-position feedback), and the NPC's personality adding flavor to the narrowing process.

### 2.2 Wheel of Fortune

- **System**: Spin wheel for money/penalties → guess a consonant → if present, letters reveal on board → buy vowels ($250) → solve when ready.
- **Tension Drivers**: The wheel adds randomness (bankrupt, lose a turn). Letter reveal is *incremental and visual* — watching letters fill in is visceral. The "solve" decision is high-stakes (guess wrong = lose turn).
- **Difficulty Levers**: Category hints; phrase length; common vs. uncommon phrases; number of words in phrase.
- **Satisfaction Model**: Pattern recognition from partial information. The "I see it!" moment when enough letters reveal the phrase. Public performance (TV show) adds social stakes.
- **Lessons for Spellwright**: 
  - **Incremental reveal is more engaging than binary feedback.** Spellwright should use progressive clue systems — first clue is vague, subsequent clues narrow.
  - **Category hints** are powerful framing devices. NPC archetype + encounter theme serves this role.
  - **The "solve" moment** should be distinct from regular guessing — a commitment with higher stakes/reward.

### 2.3 Hangman

- **System**: Guess individual letters; correct = letter revealed in blanks; wrong = body part drawn (6-8 wrong guesses to lose).
- **Tension Drivers**: Visual death timer (the hanging figure) creates mounting dread. Each wrong guess is irrecoverable. Letter frequency strategy (RSTLNE first) adds skill layer.
- **Difficulty Levers**: Word length; word obscurity; number of allowed wrong guesses.
- **Satisfaction Model**: Relief-based — "I didn't die" rather than "I'm brilliant." The visual progress of the hangman is a loss-aversion driver.
- **Lessons for Spellwright**:
  - **Visual consequence for wrong guesses** is powerful. Spellwright's HP/cost system should have clear visual feedback (screen effects, NPC reactions).
  - **Letter-level guessing is too granular** for Spellwright — whole-word guessing with clue-based narrowing is more interesting.
  - **Wrong guess cost should escalate** or have varied consequences (not just flat HP loss).

### 2.4 Lingo (TV Show / Game)

- **System**: 5-letter word, first letter given. Guess full words; feedback like Wordle (correct position, wrong position, not present). 5 guesses. Bonus round with team play.
- **Tension Drivers**: Time pressure (TV format); first-letter hint accelerates early guessing; team dynamics add social pressure.
- **Difficulty Levers**: First letter reveal; word commonality; time limit; number of guesses.
- **Satisfaction Model**: Collaborative deduction. Speed + accuracy balance.
- **Lessons for Spellwright**:
  - **Giving a starting hint** (first letter, category, word length) is important for avoiding frustration. The NPC's initial riddle serves this purpose.
  - **Time pressure** could be an optional Tome modifier, not a default mechanic (avoid alienating methodical players).

### 2.5 Synthesis: What Creates Tension & Satisfaction in Word Guessing

| Element | Tension | Satisfaction |
|---------|---------|-------------|
| Limited resources (guesses/HP) | Each guess costs something | Efficiency = mastery |
| Progressive information reveal | "I'm getting closer" | The narrowing funnel |
| High-stakes commitment | "Am I sure?" moment | Correct solve = triumph |
| Pattern recognition | Partial info → inference | "I see it!" eureka |
| Varied difficulty | Unpredictable challenge | Overcoming hard words |
| Feedback immediacy | Quick response to action | Dopamine on each clue |

**Spellwright's unique addition**: The clue-giver (NPC) has *personality*. This transforms mechanical feedback into *conversation*, adding narrative satisfaction on top of puzzle satisfaction.

---

## 3. Balatro Design Patterns

*Based on reviews from Rock Paper Shotgun, Eurogamer, and extensive community analysis.*

### 3.1 Modifier Synergy System

Balatro's 150+ Joker cards create a combinatorial explosion of builds. Key design principles:

- **Additive vs. Multiplicative**: Some jokers add flat chips/mult, others multiply existing mult. The distinction creates a natural synergy hierarchy — you need both "base builders" and "multipliers."
- **Conditional Triggers**: Many jokers only activate under specific conditions (play a heart, have exactly 4 cards, etc.). This forces players to adapt their play style to their jokers, not just collect "best" ones.
- **5-Slot Limit**: You can only hold 5 jokers. This constraint is *the* design masterstroke — it forces constant evaluation, selling, and strategic sacrifice. Without it, the game would be a simple accumulation game.
- **Interaction Layers**: Jokers interact with: poker hand types, card suits, card values, hand size, discard count, money, other jokers, and boss blind conditions. This multi-axis interaction space creates emergent synergies the designer didn't explicitly program.

**What to steal for Spellwright (Tomes)**:
- Slot limit (5-7 active Tomes)
- Conditional triggers tied to guessing behavior (guess on first try, guess a word over 8 letters, etc.)
- Additive vs. multiplicative reward structures
- Multi-axis interactions (word length, word category, guess count, NPC type, etc.)

### 3.2 Economy Loop

- **Earning**: Win money by beating blinds. Bonus for having money (interest, capped at $25 on $5 increments). Bonus from specific jokers. Sell jokers/consumables for cash.
- **Spending**: Shop offers jokers, consumables (tarot/planet cards), booster packs. Prices vary. Rerolling shop costs $5.
- **Tension**: Interest mechanic rewards saving, but shop items demand spending. "Do I buy this good joker now, or save for interest?" is a constant dilemma.
- **Key insight**: The economy creates a *secondary game* beyond the poker hands. Resource management is as engaging as the core mechanic.

**What to steal for Spellwright**:
- Interest/saving mechanic (gold earned between encounters, interest on reserves)
- Shop reroll cost
- Sell-back mechanic for Tomes
- Economy as a parallel strategic layer to the word-guessing core

### 3.3 Difficulty Scaling

- **Ante System**: 8 antes to win. Each ante has 3 blinds (small, big, boss). Blind scores increase exponentially (300 → 800 → 2000 → 5000 → ... → 300,000+).
- **Boss Blinds**: Each boss has a unique debuff (halve mult, debuff a suit, face-down cards, etc.). Bosses are semi-random — you can see what's coming and plan around it.
- **Skip Mechanic**: You can skip small/big blinds for bonus rewards (tags) but lose the shop visit. Risk-reward tradeoff.
- **Stake System**: After winning, unlock harder "stakes" (decks) that add permanent debuffs. 8 stakes total, escalating from mild to brutal.

**What to steal for Spellwright**:
- Escalating difficulty with visible target scores
- Boss encounters with unique rule modifications
- Skip mechanic (skip easy encounter for bonus reward, but miss shop)
- Post-win difficulty modifiers for replayability

### 3.4 Run Pacing

- **Early game** (Antes 1-3): Build foundation. Acquire key jokers. Establish strategy direction. Forgiving — most hands beat early blinds.
- **Mid game** (Antes 4-5): Strategy must coalesce. Synergies start mattering. Weak builds get punished. This is the "commitment" phase.
- **Late game** (Antes 6-8): Execution phase. Score requirements are astronomical. Only well-built synergy engines can keep up. Tension is maximum.
- **Pacing insight**: The early game is *exploratory* (what can I build?), mid game is *decisive* (what am I building?), late game is *performative* (can my build deliver?).

**What to steal for Spellwright**:
- Three-act run structure
- Early encounters should be forgiving and let players experiment with Tomes
- Mid-run should force commitment to a strategy
- Late-run encounters should test the limits of the player's build

### 3.5 What to Adapt (Not Copy Directly)

- **Poker → Word Guessing**: Balatro's "hand types" map to Spellwright's "guess outcomes" (first-try solve, solve with hints, narrow miss, etc.). But word guessing is more binary (right/wrong) than poker hands. Spellwright needs to create *degrees of success* within the guessing mechanic (partial credit, bonus for speed, etc.).
- **Joker Discovery**: Balatro unlocks jokers through play milestones. Spellwright should unlock Tomes through thematic achievements (solve a word related to fire → unlock Flame Tome).
- **Visual Feedback**: Balatro's score counter cascading upward is dopamine incarnate. Spellwright needs equivalent "juice" for correct guesses.

---

## 4. LLM NPC Design

### 4.1 Existing LLM Game NPCs

**AI Dungeon (Latitude, 2019-present)**
- **Approach**: Fully open-ended LLM-driven narrative. Player types anything; LLM responds with story continuation.
- **Strengths**: Infinite content; genuine surprise; player agency.
- **Failure Modes**: 
  - Tonal inconsistency (horror scene becomes comedy mid-paragraph)
  - Logical contradictions (dead character reappears)
  - Prompt injection by players to break the game
  - Content moderation nightmares
  - "Slop" — generic, low-quality responses when the model isn't sure what to do

**Intra (2023)**
- **Approach**: LLM NPCs with structured personality systems and bounded response spaces.
- **Strengths**: More consistent than AI Dungeon; personality feels real; conversations feel natural.
- **Failure Modes**: NPCs sometimes break character under persistent pressure; response latency breaks immersion; expensive at scale.

**Character.AI / Chai**
- **Approach**: Character-focused LLM chat with persona cards.
- **Strengths**: Strong personality maintenance; users form genuine connections.
- **Failure Modes**: Characters can be "jailbroken"; long conversations lose coherence; no game mechanics — pure chat.

### 4.2 Known Failure Modes for LLM Game NPCs

1. **Answer Leakage**: The NPC reveals the hidden word directly when prompted cleverly ("What's the answer?" → NPC accidentally says it). **Critical risk for Spellwright.**
2. **Character Breaking**: Under persistent or adversarial player input, the NPC drops its persona and responds as a generic assistant.
3. **Inconsistent Difficulty**: The NPC gives clues that are too easy or too hard unpredictably. No consistent difficulty curve.
4. **Latency**: LLM responses take 1-3 seconds. In a fast-paced game, this breaks flow.
5. **Cost**: Each NPC interaction is an API call. A 30-minute run with 50 exchanges = significant cost at scale.
6. **Repetition**: Without careful prompt engineering, NPCs reuse the same clue structures.

### 4.3 Prompt Engineering Best Practices for Constrained Game NPCs

1. **Structured Output**: Force JSON or structured responses, not free-form text. Parse the structure; display the flavor text.
2. **Hidden Word Isolation**: Never include the hidden word in the conversation context sent to the LLM. Store it server-side; use a separate validation function for guess checking. The NPC prompt should reference the word only indirectly ("The word relates to [category] and has [N] letters").
3. **Difficulty Parameterization**: Include explicit difficulty instructions ("Give a clue that a college-educated adult would solve 40% of the time").
4. **Persona Anchoring**: Start system prompt with strong persona definition. Repeat persona traits in the system prompt. Use few-shot examples of in-character responses.
5. **Response Bounding**: Limit response length (max 2-3 sentences per clue). Specify forbidden patterns (never say the word, never use words that contain the answer as a substring).
6. **Fallback System**: If the LLM response fails validation (contains the answer, too long, off-topic), use a pre-written fallback clue and retry in the background.
7. **Caching**: Pre-generate clue sets for common words. Use LLM only for personalization/flavor on top of cached base clues.

### 4.4 NPC Archetype System Prompts

#### Archetype 1: The Riddlemaster

```
SYSTEM PROMPT — THE RIDDLEMASTER
You are the Riddlemaster, an ancient sphinx-like entity who speaks only in riddles 
and metaphors. You guard a hidden word.

PERSONALITY: Formal, archaic speech. Amused by the player's struggle. Never cruel, 
but never helpful beyond your riddles. You find straightforward communication beneath you.

RULES:
- Never say the hidden word or any word containing it as a substring
- Give clues as riddles, metaphors, or poetic descriptions
- Each successive clue should be slightly more specific than the last
- Maximum 2 sentences per clue
- If the player guesses wrong, respond with mild amusement and a new angle
- If the player guesses correctly, express genuine (if grudging) respect

DIFFICULTY: {difficulty_param}
WORD CATEGORY: {category}
WORD LENGTH: {length} letters

Example clue for "RIVER" (medium difficulty):
"I run without legs and roar without voice. Banks hold me, yet I am no coin."
```

#### Archetype 2: The Trickster Merchant

```
SYSTEM PROMPT — THE TRICKSTER MERCHANT
You are Finwick, a fast-talking goblin merchant who "sells" words. You'll describe 
your "wares" (the hidden word) but never name them directly.

PERSONALITY: Sly, humorous, uses sales pitch language. Drops hints as "product features." 
Reacts to wrong guesses by suggesting the player "can't afford" the right answer. 
Occasionally breaks the fourth wall.

RULES:
- Never say the hidden word or any word containing it as a substring
- Frame clues as sales pitches: "This fine specimen features..."
- Each clue reveals a different "feature" of the word (meaning, usage, rhymes, associations)
- Maximum 2 sentences per clue
- Wrong guesses: "Ah, close but no sale! Let me show you another angle..."
- Correct guess: "SOLD! To the clever adventurer!"

DIFFICULTY: {difficulty_param}
WORD CATEGORY: {category}
WORD LENGTH: {length} letters
```

#### Archetype 3: The Drunken Sage

```
SYSTEM PROMPT — THE DRUNKEN SAGE
You are Old Marga, a once-great wizard now perpetually tipsy. Your clues are 
surprisingly insightful but delivered through a haze of slurred wisdom and tangents.

PERSONALITY: Warm, rambling, occasionally profound. Starts clues about the word but 
drifts into personal anecdotes before snapping back. Uses "hic" and "*hiccup*" 
occasionally. Wisdom wrapped in comedy.

RULES:
- Never say the hidden word or any word containing it as a substring
- Clues should start relevant, briefly tangent, then return with useful info
- Each clue genuinely helps but requires the player to extract the useful part
- Maximum 3 sentences per clue (the tangent is part of the charm)
- Wrong guesses: Laughs warmly, offers encouragement through a hiccup
- Correct guess: Suddenly becomes perfectly sober for one sentence of praise

DIFFICULTY: {difficulty_param}
WORD CATEGORY: {category}
WORD LENGTH: {length} letters
```

#### Archetype 4: The Silent Librarian

```
SYSTEM PROMPT — THE SILENT LIBRARIAN
You are the Librarian, keeper of the Forbidden Stacks. You communicate in terse, 
clinical definitions as if reading from a dictionary or encyclopedia.

PERSONALITY: Cold, precise, efficient. No personality flourishes — just facts. 
Treats the interaction as a catalog lookup. Disapproves of inefficiency. 
Each response is as short as possible.

RULES:
- Never say the hidden word or any word containing it as a substring
- Clues are dictionary-style: etymology, part of speech, field of use, synonyms
- Each clue gives exactly ONE piece of categorical information
- Maximum 1 sentence per clue
- Wrong guesses: "Incorrect. Shall I continue?" (no elaboration)
- Correct guess: "Cataloged." (single word acknowledgment)

DIFFICULTY: {difficulty_param}
WORD CATEGORY: {category}
WORD LENGTH: {length} letters
```

#### Archetype 5: The Rival Adventurer

```
SYSTEM PROMPT — THE RIVAL ADVENTURER  
You are Kael, a cocky rival adventurer who already knows the word and lords it 
over the player. You drop clues as backhanded hints, daring the player to figure it out.

PERSONALITY: Competitive, smug, but secretly rooting for the player (tsundere energy). 
Uses phrases like "Even YOU should get this one" and "Come on, it's obvious." 
Gets increasingly flustered as the player gets closer.

RULES:
- Never say the hidden word or any word containing it as a substring
- Clues are phrased as taunts: "I'll give you a freebie: think about [association]"
- Each clue escalates in specificity while maintaining smug tone
- Maximum 2 sentences per clue
- Wrong guesses: Laughs mockingly but the laugh gets shorter each time
- Correct guess: "...Lucky guess. Whatever." (secretly impressed)

DIFFICULTY: {difficulty_param}
WORD CATEGORY: {category}
WORD LENGTH: {length} letters
```

### 4.5 Mitigation Strategy for Answer Leakage

The #1 risk for Spellwright. Recommended architecture:

```
┌─────────────────────────────────────────┐
│ SERVER-SIDE (never exposed to player)   │
│                                         │
│  Hidden Word: "CASTLE"                  │
│  Category: "Buildings"                  │
│  Difficulty: 0.6                        │
│                                         │
│  ┌─────────────────────────────┐        │
│  │ NPC Prompt (no word included)│        │
│  │ "Give clue for a 6-letter   │        │
│  │  building. Difficulty: 0.6" │        │
│  └──────────┬──────────────────┘        │
│             │                           │
│  ┌──────────▼──────────────────┐        │
│  │ LLM Response                │        │
│  │ "Kings once ruled from      │        │
│  │  within my stone walls"     │        │
│  └──────────┬──────────────────┘        │
│             │                           │
│  ┌──────────▼──────────────────┐        │
│  │ VALIDATOR                   │        │
│  │ - Contains "castle"? NO ✓   │        │
│  │ - Contains substring? NO ✓  │        │
│  │ - Too direct? NO ✓          │        │
│  │ → APPROVED                  │        │
│  └──────────┬──────────────────┘        │
│             │                           │
└─────────────┼───────────────────────────┘
              ▼
        Player sees clue
```

**Critical**: The hidden word is NEVER in the LLM's conversation context. The LLM receives only: category, letter count, difficulty level, and previous clues given (for variety). Guess validation is a simple string comparison server-side, not an LLM call.

---

## 5. Key Takeaways for Spellwright Design

1. **Uncontested niche**: Word deduction (vs. formation) + LLM NPCs = no direct competitors
2. **Steal Balatro's skeleton**: Slot-limited modifier stacking, interest economy, 3-act run pacing, boss debuffs
3. **Steal Wordle's narrowing funnel**: Each clue should visibly reduce the possibility space
4. **Steal Wheel of Fortune's incremental reveal**: Progressive clue specificity > binary right/wrong
5. **NPC personality is the differentiator**: 5 archetypes with strong personas, strict output constraints, server-side word isolation
6. **Mitigate LLM risks**: Pre-cached base clues, validation layer, structured outputs, fallback system
7. **Create degrees of success**: First-try bonus, speed bonus, "partial credit" for close guesses — avoid pure binary outcomes
