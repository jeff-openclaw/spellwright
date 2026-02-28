# Spellwright — Email & Account Log

**Date:** 2026-02-28

---

## Trello Setup — BLOCKED

**Status:** ❌ Could not complete — browser automation unavailable

**What happened:** The OpenClaw browser control service could not connect (no attached tab). Trello signup requires browser interaction (CAPTCHA, OAuth flows, etc.) which cannot be done via `web_fetch` alone.

**Intended setup:**
- **Email:** jeff.openclaw@agentmail.to
- **Board name:** "Spellwright — MVP Sprint"
- **Lists:** To Do, In Progress, Done
- **Cards:** 12 cards based on architecture report (see 04_architecture.md §6 for development phases)

**Next steps:** Manually create the Trello board, or retry when browser automation is available. Card descriptions are documented below for easy copy-paste.

---

## Planned Trello Cards

### 1. Unity Project Setup
**Description:** Create Unity 2022.3 LTS project. Install packages: WeCantSpell.Hunspell (via NuGetForUnity), TextMeshPro, Unity MCP (git URL). Set up folder structure per architecture report §2.1. Create Main scene. Configure URP. Import fonts (IBM Plex Mono, VT323) and generate TMP font assets with Latin Extended charset.

**Acceptance criteria:**
- Project compiles with zero errors
- Folder structure matches architecture spec
- WeCantSpell.Hunspell DLL loads without errors
- Unity MCP connected to Claude Code
- TMP font assets generated for both fonts

**Dependencies:** None (first card)

---

### 2. Core Data Layer
**Description:** Create all ScriptableObject definitions: TomeData, NPCData, WordPool, GameConfig (see architecture §2.4). Create 5 TomeData assets, 3 NPCData assets with system prompts, 8 WordPool assets with ~60 words each (~500 total). Load en_US.dic via WeCantSpell.Hunspell from StreamingAssets. Implement WordValidator wrapper.

**Acceptance criteria:**
- All SO types created and compile
- Sample data assets created and inspectable in editor
- WordValidator.IsValidEnglishWord() works in play mode
- 500 curated words across 8 categories with difficulty 1-5

**Dependencies:** #1 Unity Project Setup

---

### 3. Ollama Service
**Description:** Implement OllamaService (architecture §4.1): HttpClient-based, async/await. Support streaming (NDJSON parsing) and non-streaming (JSON mode) requests. Request queue with semaphore (1 concurrent). Timeout handling (15s). Fallback chain: primary model → fallback model → static emergency clues. Verify model availability at startup via GET /api/tags.

**Acceptance criteria:**
- Can send chat completion to Ollama and receive response
- Streaming mode delivers tokens via callback
- JSON mode returns parsed structured response
- Timeout triggers fallback to secondary model
- Connection failure shows error, doesn't crash
- Emergency static clues work when Ollama is completely down

**Dependencies:** #1 Unity Project Setup

---

### 4. NPC Prompt System
**Description:** Implement PromptBuilder (architecture §4.4): assembles system prompt from NPCData + target word + category + clue number + previous guesses + Tome modifiers. Implement ResponseParser: JSON parsing primary, regex fallback. Implement clue sanitization (§4.6) — detect and reject clues containing the target word. Create 3 NPC system prompts: Riddlemaster (oblique riddles), Trickster Merchant (sales pitch style), Silent Librarian (clinical definitions).

**Acceptance criteria:**
- PromptBuilder produces well-formed prompts for all 3 archetypes
- Each NPC produces noticeably different clue styles
- Clue sanitization catches target word in output
- JSON parsing works; regex fallback works when JSON fails
- Successive clues for same word get progressively more specific

**Dependencies:** #2 Core Data Layer, #3 Ollama Service

---

### 5. Word Guessing Encounter
**Description:** Implement EncounterManager: word selection (filter by difficulty + length + category, no repeats per run), guess loop (submit → validate via Hunspell → check match → feedback → next clue or victory), HP deduction on wrong guess, score calculation (base × guess multiplier). Implement GuessProcessor for input validation. Wire to OllamaService for clue generation.

**Acceptance criteria:**
- Player can play a full encounter: see blanks, read clue, type guess, get feedback
- Correct guess ends encounter with score
- Wrong guess costs HP and triggers new clue
- Invalid English words rejected with "not a word" feedback
- Word selected from pool matching difficulty/length criteria
- No word repeats within a run

**Dependencies:** #2 Core Data Layer, #3 Ollama Service, #4 NPC Prompt System

---

### 6. Tome/Modifier System
**Description:** Implement TomeSystem: manages 5 slots, fires hooks at encounter events (OnEncounterStart, OnWrongGuess, OnCorrectGuess, OnClueGenerated). Define ITomeEffect interface. Implement 5 MVP Tomes: Vowel Lens (reveal vowels after first wrong guess), First Light (reveal first letter always), Echo Chamber (show which guessed letters appear in answer), Thick Skin (+10 max HP), Second Wind (one free wrong guess per encounter).

**Acceptance criteria:**
- TomeSystem correctly manages equip/unequip with 5-slot limit
- Each of 5 Tomes triggers at correct encounter event
- Vowel Lens: vowel positions highlighted after first wrong guess
- First Light: first letter shown at encounter start
- Echo Chamber: letter overlap feedback shown after wrong guess
- Thick Skin: max HP increased by 10
- Second Wind: first wrong guess per encounter costs 0 HP

**Dependencies:** #2 Core Data Layer, #5 Encounter

---

### 7. Run Manager & Map
**Description:** Implement RunManager: tracks run state (HP, gold, score, equipped Tomes, node index). Generates linear node sequence on run start: 8-10 nodes (E-E-S-E-E-E-S-E-B pattern — encounters, shops, boss). Implement MapManager: displays node list, highlights current, handles navigation. Implement GameManager state machine: MainMenu → RunSetup → Map → Encounter/Shop/Boss → RunEnd.

**Acceptance criteria:**
- New run generates valid node sequence with boss at end
- Map UI shows all nodes, current highlighted
- Player can advance through nodes in order
- HP, gold, score persist across encounters
- Run ends on HP=0 (death) or boss victory (win)
- State transitions work: menu → run → map → encounter → map → ... → end → menu

**Dependencies:** #5 Encounter, #6 Tomes

---

### 8. Shop System
**Description:** Implement ShopManager: generates shop inventory (2-3 random Tomes from pool + 1 healing item at 5g=5HP). Display Tome cards with name, description, price. Buy = deduct gold, add to TomeSystem. Can sell equipped Tomes for 50% price or 3g if found. Tome prices: Common 5-8g, Uncommon 10-15g.

**Acceptance criteria:**
- Shop shows random Tome offerings with prices
- Can buy Tome if gold sufficient (gold deducted, Tome equipped)
- Can't buy if not enough gold or slots full
- Can buy healing (HP increases, gold deducted, respects max HP)
- Can sell equipped Tomes
- Shop inventory varies between visits

**Dependencies:** #2 Core Data Layer, #6 Tomes, #7 Run Manager

---

### 9. Boss Encounter
**Description:** Implement The Whisperer boss: same as standard encounter but clues are limited to 3 words. Modify PromptBuilder to append "Your clue must be EXACTLY 3 words. No more." to system prompt for boss encounters. Add post-processing to truncate clues to 3 words if LLM exceeds limit. Boss uses difficulty 3-4 words. Show boss intro with distinct UI treatment.

**Acceptance criteria:**
- Boss encounter uses 3-word clue constraint
- LLM consistently produces ~3 word clues (or truncated to 3)
- Boss word is harder than average encounter
- Boss has distinct UI indicator (different color/label)
- Beating boss completes the run (victory screen)

**Dependencies:** #4 NPC Prompt System, #5 Encounter, #7 Run Manager

---

### 10. CRT Visual Layer
**Description:** Create custom URP Renderer Feature for CRT post-processing: scanlines (horizontal darkening), barrel distortion (curved screen), phosphor grid (RGB subpixel mask), chromatic aberration, vignette. Single-pass full-screen shader (~60-80 lines HLSL). Add toggle to disable for accessibility. Reference: architecture report §2.1 Shaders/CRT/.

**Acceptance criteria:**
- CRT effect visible on all UI (full-screen post-process)
- Scanlines, barrel distortion, chromatic aberration all present
- Effect is subtle enough to not impair readability
- Can be toggled off
- No measurable FPS impact (<1ms on RX 6600 XT)

**Dependencies:** #1 Unity Project Setup

---

### 11. Encounter UI & Juice
**Description:** Build EncounterScreen UI: word blanks display (monospace letter tiles), text input field, NPC dialogue area with typewriter effect, HP bar, gold counter, guess history, active Tomes display. Add juice: screen shake on wrong guess (Cinemachine impulse or manual), letter reveal animation, damage flash, NPC portrait/name plate with theme color.

**Acceptance criteria:**
- All encounter info visible: blanks, HP, gold, NPC, clue, guess history, Tomes
- Typewriter effect on NPC dialogue (per-character reveal)
- Screen shake on wrong guess
- Smooth letter reveal animation on correct positions
- HP bar animates on damage
- UI uses IBM Plex Mono (body) + VT323 (headers)

**Dependencies:** #5 Encounter, #10 CRT Visual Layer

---

### 12. Integration & Playtest
**Description:** Wire all systems together for a complete run: main menu → start run → traverse 8-10 nodes → encounter NPCs → visit shops → fight boss → win/lose screen. Tune difficulty: word difficulty per node position, HP cost, gold economy, Tome power level. Validate core loop hypothesis: is the clue→guess→reward cycle fun? Fix bugs. Target: 15-25 minute complete run.

**Acceptance criteria:**
- Can play a full run start to finish without crashes
- Run takes 15-25 minutes
- Difficulty feels progressive (early=easy, late=hard)
- All 3 NPC archetypes feel distinct
- All 5 Tomes meaningfully change encounter strategy
- LLM clues don't leak target words
- Gold economy feels balanced (can buy 2-3 Tomes per run)

**Dependencies:** All previous cards
