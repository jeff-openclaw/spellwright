# Spellwright — Architecture Report

**Date:** 2026-02-28  
**Based on:** [01_technical.md](./01_technical.md), [03_game_design_document.md](./03_game_design_document.md)

---

## 1. MVP Scope Definition

### What We're Proving

One question: **Is the core loop of "guess words from LLM-generated clues while collecting modifiers" fun?**

Everything in the MVP exists to answer that. Everything else is cut.

### IN (MVP)

| Feature | Spec |
|---------|------|
| **Core guessing mechanic** | NPC gives clue → player types guess → feedback → next clue or victory |
| **Language** | English only (en_US dictionary) |
| **Tomes** | 5 modifiers: Vowel Lens, First Light, Echo Chamber, Thick Skin, Second Wind |
| **NPC archetypes** | 3 types: Riddlemaster, Trickster Merchant, Silent Librarian |
| **Run structure** | Linear path, 5-8 encounters + 1 boss (The Whisperer: 3-word clues) |
| **Shop** | 1 shop node per run: buy Tomes + healing |
| **CRT aesthetic** | Custom shader (scanlines, barrel distortion, phosphor), IBM Plex Mono + VT323 |
| **LLM integration** | Ollama local inference, Qwen 2.5 7B primary, Llama 3.2 3B fallback |
| **HP system** | 30 HP, 1 HP per wrong guess, game over at 0 |
| **Scoring** | Base score × guess bonus, flat gold reward |
| **Word pool** | ~500 curated English words, 4-7 letters, categorized |
| **Platform** | Desktop (Windows/Linux), Unity 2022.3 LTS |

### OUT (MVP)

Multilingual, meta-progression, save/load, Steam integration, audio/music, branching map paths, events, rest nodes, difficulty modifiers, Tome upgrades, rare/legendary Tomes, interest/economy, word embeddings for semantic closeness, Rival & Sage NPC archetypes.

---

## 2. System Architecture

### 2.1 Folder Structure

```
Assets/
├── Spellwright/
│   ├── Scripts/
│   │   ├── Core/              # GameManager, StateMachine, Events
│   │   ├── Run/               # RunManager, MapManager, NodeData
│   │   ├── Encounter/         # EncounterManager, GuessProcessor, NPCController
│   │   ├── LLM/               # OllamaService, PromptBuilder, ResponseParser
│   │   ├── Words/             # WordValidator, WordPool, DictionaryLoader
│   │   ├── Tomes/             # TomeSystem, TomeEffects, individual tome scripts
│   │   ├── Shop/              # ShopManager, ShopUI
│   │   ├── UI/                # UIManager, screens, widgets, TypewriterEffect
│   │   └── Data/              # ScriptableObject definitions
│   ├── Data/
│   │   ├── Tomes/             # TomeData assets (.asset files)
│   │   ├── NPCs/              # NPCData assets
│   │   ├── WordPools/         # WordPool assets
│   │   └── Config/            # GameConfig asset (tuning values)
│   ├── Scenes/
│   │   └── Main.unity         # Single scene, UI-driven state changes
│   ├── UI/
│   │   ├── Prefabs/           # UI screen prefabs
│   │   └── Fonts/             # IBM Plex Mono, VT323 + TMP font assets
│   ├── Shaders/
│   │   └── CRT/               # CRT post-process shader + renderer feature
│   └── StreamingAssets/
│       └── Dictionaries/      # en_US.dic, en_US.aff
├── Plugins/                   # WeCantSpell.Hunspell DLL
└── TextMesh Pro/              # TMP essentials
```

### 2.2 Namespaces

```
Spellwright.Core          — GameManager, state machine, event bus
Spellwright.Run           — RunManager, map, node types
Spellwright.Encounter     — EncounterManager, guess processing, NPC controller
Spellwright.LLM           — OllamaService, prompt building, response parsing
Spellwright.Words         — Dictionary, word pools, validation
Spellwright.Tomes         — TomeSystem, TomeEffect base, individual effects
Spellwright.Shop          — Shop logic
Spellwright.UI            — All UI code
Spellwright.Data          — ScriptableObject types
```

### 2.3 Core Systems

#### GameManager (Singleton)
- Owns the top-level state machine
- Routes state transitions, holds references to all managers
- States: `MainMenu`, `RunSetup`, `Map`, `Encounter`, `Shop`, `Boss`, `RunEnd`

#### RunManager
- Tracks current run state: HP, gold, score, equipped Tomes, current node index
- Generates linear node sequence on run start (e.g., E-E-S-E-E-E-S-E-B)
- Provides `AdvanceToNextNode()`, `EndRun()`

#### EncounterManager
- Orchestrates a single encounter: selects word, initializes NPC, manages guess loop
- Holds encounter state: target word, guesses made, clues given, active Tome effects
- Delegates to `GuessProcessor` for validation and feedback
- Calls `OllamaService` for clue generation

#### OllamaService
- See §4 below (dedicated section)

#### WordValidator
- Wraps WeCantSpell.Hunspell: `bool IsValidEnglishWord(string word)`
- Loaded once at startup from StreamingAssets
- Also provides `Suggest(string)` for "did you mean?" UX

#### TomeSystem
- Manages equipped Tomes (max 5 slots)
- Fires hooks at encounter events: `OnEncounterStart`, `OnWrongGuess`, `OnCorrectGuess`, `OnClueGenerated`
- Each Tome implements `ITomeEffect` interface with relevant hook methods
- Tome effects modify encounter state (reveal letters, adjust HP cost, modify score)

#### UIManager
- Manages UI screen stack (push/pop pattern)
- Screens: MainMenuScreen, MapScreen, EncounterScreen, ShopScreen, RewardScreen, RunEndScreen
- Single scene architecture — all screens are prefabs enabled/disabled

### 2.4 ScriptableObject Definitions

#### TomeData
```csharp
[CreateAssetMenu(menuName = "Spellwright/Tome")]
public class TomeData : ScriptableObject
{
    public string tomeName;
    public string description;
    public TomeRarity rarity;          // Common, Uncommon (MVP only)
    public TomeCategory category;      // Insight, Economy, Defense, Offense, Meta
    public Sprite icon;
    public int shopPrice;
    public string effectClassName;     // Resolved via reflection or enum to instantiate ITomeEffect
}
```

#### NPCData
```csharp
[CreateAssetMenu(menuName = "Spellwright/NPC")]
public class NPCData : ScriptableObject
{
    public string archetypeName;       // "Riddlemaster", "Merchant", "Librarian"
    public string displayName;         // "Eldritch the Riddlemaster"
    [TextArea(5, 20)]
    public string systemPrompt;        // Full LLM system prompt for this archetype
    public string[] greetingTemplates; // Random intro lines (no LLM needed)
    public float difficultyModifier;   // Multiplier on word difficulty selection
    public Color themeColor;           // UI accent color for this NPC
}
```

#### WordPool
```csharp
[CreateAssetMenu(menuName = "Spellwright/WordPool")]
public class WordPool : ScriptableObject
{
    public string category;            // "Animals", "Emotions", "Tools", etc.
    public WordEntry[] words;
}

[System.Serializable]
public class WordEntry
{
    public string word;
    public int difficulty;             // 1-5 scale derived from frequency
    public int letterCount;            // Cached for quick filtering
}
```

#### GameConfig
```csharp
[CreateAssetMenu(menuName = "Spellwright/GameConfig")]
public class GameConfig : ScriptableObject
{
    public int startingHP = 30;
    public int maxHP = 40;
    public int maxTomeSlots = 5;
    public int hpCostPerWrongGuess = 1;
    public int baseEncounterGold = 10;
    public float[] guessMultipliers = { 3f, 2f, 1.5f, 1f }; // By guess number
    public string ollamaBaseUrl = "http://localhost:11434";
    public string primaryModel = "qwen2.5:7b";
    public string fallbackModel = "llama3.2:3b";
    public float llmTimeoutSeconds = 15f;
}
```

### 2.5 State Flow

```
MainMenu
  │
  ▼
RunSetup ─── Generate node sequence, reset HP/gold/tomes
  │
  ▼
Map ─── Show linear node list, highlight current
  │
  ├──▶ Encounter ─── Word select → clue loop → reward
  │       │
  │       ▼
  │    RewardScreen ─── Pick Tome or skip → back to Map
  │
  ├──▶ Shop ─── Buy tomes/healing → back to Map
  │
  ├──▶ Boss ─── Same as Encounter but with rule twist → RewardScreen
  │
  ▼
RunEnd ─── Win/death screen, stats, "Play Again"
  │
  ▼
MainMenu
```

### 2.6 Event Bus

Lightweight publish/subscribe for decoupling:

```csharp
public static class GameEvents
{
    public static event Action<string> OnGuessSubmitted;
    public static event Action<bool, string> OnGuessResult;   // correct, feedback
    public static event Action<string> OnClueReceived;
    public static event Action<int> OnHPChanged;
    public static event Action<int> OnGoldChanged;
    public static event Action<TomeData> OnTomeEquipped;
    public static event Action<TomeData> OnTomeRemoved;
    public static event Action OnEncounterStarted;
    public static event Action<bool> OnEncounterEnded;        // won
    public static event Action OnRunEnded;
}
```

---

## 3. Data Pipeline — Word Selection

### 3.1 Flow

```
1. Run starts → load WordPool ScriptableObjects (all categories)
2. Encounter starts:
   a. Determine difficulty range from current Act/node position
   b. Filter words by difficulty range (e.g., difficulty 1-2 for early encounters)
   c. Filter by letter count range (e.g., 4-5 letters early, 6-7 later)
   d. Optionally filter by category (if variety needed — avoid repeats)
   e. Random select from filtered pool
   f. Mark word as "used this run" (no repeats)
3. Send word + category + NPC archetype to PromptBuilder
4. PromptBuilder assembles prompt → OllamaService generates clue
5. Clue returned to EncounterManager → displayed via TypewriterEffect
```

### 3.2 Dictionary Loading Strategy

**Two separate systems:**

1. **WordPool (ScriptableObjects)** — The curated game word list (~500 words for MVP). Pre-authored with difficulty ratings and categories. This is what the game picks target words from.

2. **WeCantSpell.Hunspell dictionary** — Used only for *validating player guesses*. Loaded once at startup from `StreamingAssets/Dictionaries/en_US.dic`. Ensures players can only guess real English words (prevents random letter spam).

These are intentionally separate: the game word pool is hand-curated for fun, while Hunspell covers the full English language for input validation.

### 3.3 Word Frequency → Difficulty Mapping

For the MVP's 500-word curated list, difficulty is manually assigned (1-5 scale):

| Difficulty | Criteria | Examples |
|------------|----------|----------|
| 1 | Top 500 most common, 4-5 letters | "house", "water", "light" |
| 2 | Top 2000, 4-6 letters | "flame", "shield", "brave" |
| 3 | Top 5000, 5-7 letters | "beacon", "throne", "cipher" |
| 4 | Top 10000, 6-7 letters | "lantern", "crimson", "phantom" |
| 5 | Uncommon, 6-8+ letters | "obsidian", "quixotic", "verdant" |

**Post-MVP enhancement:** Automate difficulty scoring using word frequency lists (e.g., SUBTLEX-US corpus) and letter pattern complexity.

### 3.4 Category Design (MVP)

8 categories, ~60 words each:

- Animals, Nature, Objects, Actions, Emotions, Food & Drink, Places, Body & Health

Categories are shown to the player as hints. NPC clues must stay within the category's semantic space.

---

## 4. LLM Integration Architecture

### 4.1 OllamaService Class Design

```csharp
namespace Spellwright.LLM
{
    public class OllamaService
    {
        // --- Configuration ---
        private string _baseUrl;
        private string _primaryModel;
        private string _fallbackModel;
        private float _timeoutSeconds;
        private HttpClient _http;

        // --- Request Queue ---
        private ConcurrentQueue<ClueRequest> _requestQueue;
        private SemaphoreSlim _concurrencyLimiter = new(1); // 1 at a time

        // --- Core Methods ---

        // Fire-and-forget pre-generation (queues request)
        public void PreGenerateClue(ClueRequest request);

        // Blocking request with streaming callback
        public async Task<ClueResponse> GenerateClue(
            ClueRequest request,
            Action<string> onToken = null,      // For typewriter streaming
            CancellationToken ct = default
        );

        // --- Internal ---
        private async Task<ClueResponse> SendRequest(
            string model, ChatMessage[] messages, bool stream,
            Action<string> onToken, CancellationToken ct
        );

        private async Task<ClueResponse> FallbackRequest(ClueRequest request);
    }

    public class ClueRequest
    {
        public string TargetWord;
        public string Category;
        public NPCData NPC;
        public int ClueNumber;          // 1st clue, 2nd clue, etc.
        public string[] PreviousGuesses;
        public TomeData[] ActiveTomes;  // Some Tomes modify prompts (e.g., Rhyme Scheme)
    }

    public class ClueResponse
    {
        public string ClueText;
        public bool UsedFallbackModel;
        public float GenerationTimeMs;
        public bool FromCache;          // Pre-generated
    }
}
```

### 4.2 Error Handling & Fallback Chain

```
1. Send request to primary model (Qwen 2.5 7B)
   ├── Success → parse response → return
   ├── Timeout (15s) → try fallback model
   ├── Connection refused (Ollama not running) → show error UI
   └── Malformed response → retry once, then fallback

2. Fallback to secondary model (Llama 3.2 3B)
   ├── Success → parse response → return (flag UsedFallbackModel)
   ├── Timeout (15s) → use emergency static clue
   └── Failure → use emergency static clue

3. Emergency static clues (no LLM)
   - Pre-written generic clues per category
   - "This [category] word has [N] letters and starts with [first letter]"
   - Functional but not fun — indicates Ollama is down
```

### 4.3 Pre-Generation Strategy

To hide LLM latency, generate clues before the player needs them:

```
1. When player enters Map screen:
   → Pre-generate first clue for the NEXT encounter's word
   → Store in ClueCache (Dictionary<string, ClueResponse>)

2. When encounter starts:
   → Check cache. If hit, display immediately.
   → If miss, generate synchronously (with loading indicator).

3. After player makes a wrong guess:
   → Immediately request next clue (while showing feedback animation)
   → By the time feedback animation finishes (~1.5s), clue is likely ready

4. Pre-generation is best-effort:
   → If player navigates fast, cache miss is fine
   → Cache is keyed on (word + NPC + clueNumber)
```

### 4.4 Prompt Assembly

#### System Prompt Template (per NPC)
```
You are {npc.displayName}, a {npc.archetypeName} in a magical word-guessing game.

{npc.systemPrompt}

RULES:
- The player is trying to guess a secret word. You give clues.
- NEVER say the word directly. NEVER use the word in your clue.
- NEVER use a word that contains the secret word as a substring.
- Your clue should be 1-2 sentences maximum.
- The word category is "{category}".
- This is clue #{clueNumber}. Each clue should be MORE specific than the last.
{tomeModifiers}

Respond in JSON format:
{{"clue": "your clue text", "difficulty_hint": "easy|medium|hard"}}
```

#### User Prompt (per clue request)
```
The secret word is "{targetWord}" (category: {category}, {letterCount} letters).
{previousGuessContext}
Give clue #{clueNumber}. Remember: more specific than previous clues.
```

Where `previousGuessContext` is:
```
The player has guessed: "flame" (wrong), "blaze" (wrong).
They are getting closer/further based on these guesses.
```

### 4.5 Response Parsing

**Primary: JSON mode** (Ollama `format` parameter)

```csharp
// Request with structured output
var payload = new {
    model = modelName,
    messages = messages,
    format = new {
        type = "object",
        properties = new {
            clue = new { type = "string" },
            difficulty_hint = new { type = "string" }
        },
        required = new[] { "clue", "difficulty_hint" }
    },
    stream = false,  // JSON mode: don't stream, get complete response
    options = new { temperature = 0.8, num_predict = 150 }
};
```

**Fallback: Regex extraction**

If JSON parsing fails, extract clue from raw text:
```csharp
// Try JSON first
if (TryParseJson(rawResponse, out ClueResponse result))
    return result;

// Fallback: take first 1-2 sentences as the clue
var sentences = rawResponse.Split(new[] { '.', '!', '?' });
var clue = string.Join(". ", sentences.Take(2)).Trim();
return new ClueResponse { ClueText = clue };
```

### 4.6 Safety: Word Leakage Prevention

The target word is sent to Ollama (local, on-device — no network exfiltration risk). But the LLM might accidentally include the word in its clue. Post-processing:

```csharp
public string SanitizeClue(string clue, string targetWord)
{
    // Check for exact match (case-insensitive)
    if (clue.Contains(targetWord, StringComparison.OrdinalIgnoreCase))
        return RegenerateOrFallback();

    // Check for substring match (e.g., target "cat" in "concatenate")
    // Only flag if target appears as a standalone word
    var pattern = $@"\b{Regex.Escape(targetWord)}\b";
    if (Regex.IsMatch(clue, pattern, RegexOptions.IgnoreCase))
        return RegenerateOrFallback();

    return clue;
}
```

### 4.7 Model Management

```csharp
// Use Ollama's keep_alive to manage VRAM
// After generating, keep model loaded for 5 minutes (expect more requests)
var options = new { keep_alive = "5m" };

// When entering shop/map (no LLM needed soon), unload:
var unload = new { keep_alive = "0" };

// Check available models at startup:
// GET /api/tags → verify primary + fallback are pulled
```

---

## 5. Key Technical Risks

| Risk | Mitigation |
|------|-----------|
| LLM clues leak the target word | Post-generation sanitization (§4.6); re-roll or fallback to static clue |
| LLM latency too high for game feel | Pre-generation (§4.3); streaming typewriter hides partial latency; fallback to faster 3B model |
| ROCm instability on RX 6600 XT | CPU fallback works for 3B model; test early, document GPU override setup |
| Clue quality inconsistent | Structured JSON mode + strong system prompts; playtest and iterate prompts |
| WeCantSpell.Hunspell performance in Unity | Pure managed C#, benchmarks show <1ms per check; load once at startup |
| Single-scene UI complexity | Strict screen stack pattern; each screen is self-contained prefab |

---

## 6. Development Order (Recommended)

```
Phase 1 — Foundation (Week 1-2)
  ├── Unity project setup, packages, folder structure
  ├── GameManager + state machine
  ├── ScriptableObject definitions + sample data
  └── Dictionary loading + word validation

Phase 2 — Core Loop (Week 3-4)
  ├── OllamaService + prompt builder
  ├── EncounterManager + guess processing
  ├── Basic encounter UI (blanks, input, clue display)
  └── Single encounter playable end-to-end

Phase 3 — Systems (Week 5-6)
  ├── TomeSystem + 5 MVP Tomes
  ├── RunManager + linear map
  ├── Shop system
  └── Boss encounter (The Whisperer)

Phase 4 — Polish (Week 7-8)
  ├── CRT shader + visual effects
  ├── Typewriter effect + screen shake
  ├── UI polish (all screens)
  └── Integration testing + difficulty tuning
```

---

*This architecture is designed to be extended. Every system uses interfaces/events for loose coupling. Adding new Tomes, NPCs, or encounter types should never require modifying core systems.*
