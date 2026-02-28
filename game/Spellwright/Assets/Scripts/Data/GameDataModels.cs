using System;
using System.Collections.Generic;

namespace Spellwright.Data
{
    // ── Enums ──────────────────────────────────────────────

    /// <summary>Types of nodes on the run map.</summary>
    public enum NodeType
    {
        Encounter,
        Shop,
        Boss,
        Rest,
        Event
    }

    /// <summary>Rarity tier for Tomes.</summary>
    public enum TomeRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>NPC personality archetype that drives prompt selection.</summary>
    public enum NPCArchetype
    {
        Riddlemaster,
        TricksterMerchant,
        SilentLibrarian
    }

    /// <summary>Category of a Tome's effect.</summary>
    public enum TomeCategory
    {
        Insight,
        Economy,
        Defense,
        Offense,
        Meta
    }

    // ── Data Classes ───────────────────────────────────────

    /// <summary>Parsed response from the LLM clue generation.</summary>
    public class ClueResponse
    {
        public string Clue { get; set; }
        public string Mood { get; set; }
        public bool UsedFallbackModel { get; set; }
        public float GenerationTimeMs { get; set; }
    }

    /// <summary>Result of evaluating a player's guess.</summary>
    public class GuessResult
    {
        public string GuessedWord { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsValidWord { get; set; }
        public string Feedback { get; set; }
        public int LettersCorrect { get; set; }
    }

    /// <summary>Mutable state of an in-progress run.</summary>
    public class RunState
    {
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int Gold { get; set; }
        public int Score { get; set; }
        public int CurrentNodeIndex { get; set; }
        public List<NodeType> NodeSequence { get; set; } = new List<NodeType>();
        public List<string> UsedWords { get; set; } = new List<string>();
        public List<TomeInstance> EquippedTomes { get; set; } = new List<TomeInstance>();
        public bool IsRunActive { get; set; }
    }

    /// <summary>Runtime instance of an equipped Tome.</summary>
    public class TomeInstance
    {
        public string TomeId { get; set; }
        public string TomeName { get; set; }
        public TomeRarity Rarity { get; set; }
        public TomeCategory Category { get; set; }
        public string EffectClassName { get; set; }
    }

    /// <summary>Prompt data passed to PromptBuilder for NPC clue generation.</summary>
    public class NPCPromptData
    {
        public string DisplayName { get; set; }
        public NPCArchetype Archetype { get; set; }
        public string SystemPromptTemplate { get; set; }
        public float DifficultyModifier { get; set; }
        public bool IsBoss { get; set; }
        /// <summary>Boss constraint, e.g. "Your clues must be exactly 3 words."</summary>
        public string BossConstraint { get; set; }
    }

    /// <summary>A word entry from the curated word pool.</summary>
    public class WordEntry
    {
        public string Word { get; set; }
        public string Category { get; set; }
        public int Difficulty { get; set; }
        public int LetterCount { get; set; }
    }

    // ── Event Payloads ─────────────────────────────────────

    /// <summary>Fired when an encounter begins.</summary>
    public class EncounterStartedEvent
    {
        public string TargetWord { get; set; }
        public string Category { get; set; }
        public NPCPromptData NPC { get; set; }
    }

    /// <summary>Fired when the player submits a guess.</summary>
    public class GuessSubmittedEvent
    {
        public string Guess { get; set; }
        public GuessResult Result { get; set; }
    }

    /// <summary>Fired when a clue is received from the LLM.</summary>
    public class ClueReceivedEvent
    {
        public ClueResponse Clue { get; set; }
        public int ClueNumber { get; set; }
    }

    /// <summary>Fired when an encounter ends.</summary>
    public class EncounterEndedEvent
    {
        public bool Won { get; set; }
        public string TargetWord { get; set; }
        public int GuessCount { get; set; }
        public int Score { get; set; }
    }

    /// <summary>Fired when a Tome is equipped.</summary>
    public class TomeEquippedEvent
    {
        public TomeInstance Tome { get; set; }
        public int SlotIndex { get; set; }
    }

    /// <summary>Fired when a Tome is removed.</summary>
    public class TomeRemovedEvent
    {
        public TomeInstance Tome { get; set; }
    }

    /// <summary>Fired when player HP changes.</summary>
    public class HPChangedEvent
    {
        public int OldHP { get; set; }
        public int NewHP { get; set; }
        public int MaxHP { get; set; }
    }

    /// <summary>Fired when player gold changes.</summary>
    public class GoldChangedEvent
    {
        public int OldGold { get; set; }
        public int NewGold { get; set; }
    }
}
