using System;
using System.Collections.Generic;

namespace Spellwright.Data
{
    // ── Enums ──────────────────────────────────────────────

    /// <summary>Supported game languages for clues, feedback, and word pools.</summary>
    public enum GameLanguage
    {
        English,
        Romanian
    }

    /// <summary>Types of nodes on the run map.</summary>
    public enum NodeType
    {
        Encounter,
        Shop,
        Boss,
        Rest,
        Event,
        DeadDrop
    }

    /// <summary>High-level game flow states.</summary>
    public enum GameState
    {
        MainMenu,
        RunSetup,
        Map,
        Encounter,
        Shop,
        Boss,
        RunEnd
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
        SilentLibrarian,
        Guide
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

    /// <summary>Whether a guess is a single letter or a full phrase attempt.</summary>
    public enum GuessType
    {
        Letter,
        Phrase
    }

    /// <summary>Adaptive difficulty shift based on NPC mood.</summary>
    public enum DifficultyShift
    {
        Normal,
        Mercy,
        Cruel
    }

    /// <summary>Type of board reveal requested by a Tome effect.</summary>
    public enum RevealType
    {
        FirstLetter,
        Vowels,
        SpecificLetters,
        Random
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
        public GuessType GuessType { get; set; }
        public char GuessedLetter { get; set; }
        public int LetterOccurrences { get; set; }
        public bool IsLetterInPhrase { get; set; }
        public bool IsLetterAlreadyGuessed { get; set; }
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
        public bool IsPhrase { get; set; }
        public int WordCount { get; set; } = 1;
    }

    // ── Event Payloads ─────────────────────────────────────

    /// <summary>Fired when an encounter begins.</summary>
    public class EncounterStartedEvent
    {
        public string TargetWord { get; set; }
        public string Category { get; set; }
        public NPCPromptData NPC { get; set; }
        public bool IsPhrase { get; set; }
        public int WordCount { get; set; }
        public int LetterCount { get; set; }
        public bool IsFirstEncounter { get; set; }
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
        public bool IsBoss { get; set; }
        public string TargetWord { get; set; }
        public int GuessCount { get; set; }
        public int Score { get; set; }
    }

    /// <summary>Fired before the first clue in a boss encounter.</summary>
    public class BossIntroEvent
    {
        public string BossName { get; set; }
        public string IntroText { get; set; }
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

    /// <summary>Fired when a Tome effect triggers during an encounter.</summary>
    public class TomeTriggeredEvent
    {
        public string TomeName { get; set; }
        public string Description { get; set; }
        public string RevealedInfo { get; set; }
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

    /// <summary>Fired when a new run begins.</summary>
    public class RunStartedEvent
    {
        public RunState State { get; set; }
    }

    /// <summary>Fired when a run ends (win, loss, or HP depletion).</summary>
    public class RunEndedEvent
    {
        public int FinalScore { get; set; }
        public bool Won { get; set; }
    }

    /// <summary>Fired when run-level state changes (score, gold, node progression).</summary>
    public class RunStateChangedEvent
    {
        public RunState State { get; set; }
    }

    /// <summary>Fired when the game state machine transitions.</summary>
    public class GameStateChangedEvent
    {
        public GameState OldState { get; set; }
        public GameState NewState { get; set; }
    }

    /// <summary>Fired when a map node is selected for play.</summary>
    public class NodeSelectedEvent
    {
        public int NodeIndex { get; set; }
        public NodeType NodeType { get; set; }
    }

    /// <summary>Fired when letter tiles are revealed on the board.</summary>
    public class LetterRevealedEvent
    {
        public List<int> RevealedPositions { get; set; }
        public char RevealedLetter { get; set; }
        /// <summary>"clue", "guess", "consolation", or "tome"</summary>
        public string Source { get; set; }
    }

    /// <summary>Fired when a rival NPC is designated at run start.</summary>
    public class RivalDesignatedEvent
    {
        public NPCPromptData Rival { get; set; }
    }

    /// <summary>Fired when a rival encounter begins.</summary>
    public class RivalEncounterStartedEvent
    {
        public int Tier { get; set; }
    }

    /// <summary>Fired when the player defeats the rival NPC.</summary>
    public class RivalDefeatedEvent
    {
        public int Tier { get; set; }
    }

    /// <summary>Fired when the player reaches their final guess, triggering the ultimatum sequence.</summary>
    public class UltimatumTriggeredEvent
    {
        public string NpcName { get; set; }
        public string Mood { get; set; }
    }

    /// <summary>Fired when the ultimatum countdown expires without a guess.</summary>
    public class UltimatumExpiredEvent { }

    /// <summary>Fired when the NPC delivers their ultimatum line.</summary>
    public class UltimatumLineReceivedEvent
    {
        public string Line { get; set; }
    }

    /// <summary>Fired when the adaptive difficulty shift changes based on NPC mood.</summary>
    public class DifficultyShiftChangedEvent
    {
        public DifficultyShift Shift { get; set; }
    }

    // ── Bargain Events ────────────────────────────────────

    /// <summary>The type of effect a bargain applies.</summary>
    public enum BargainEffect { RevealVowel, SkipGuess, DoubleRisk, HealSmall }

    /// <summary>Fired when the NPC offers a mood bargain to the player.</summary>
    public class BargainOfferedEvent
    {
        public string Description { get; set; }
        public string CostDescription { get; set; }
        public string NpcFlavorText { get; set; }
        public BargainEffect Effect { get; set; }
        public string NpcName { get; set; }
    }

    /// <summary>Fired when the player accepts a bargain offer.</summary>
    public class BargainAcceptedEvent
    {
        public BargainEffect Effect { get; set; }
    }

    /// <summary>Fired when a bargain offer expires without being accepted.</summary>
    public class BargainExpiredEvent { }

    // ── Letter Sacrifice Events ─────────────────────────────

    /// <summary>Fired when the player sacrifices a revealed letter for a better clue.</summary>
    public class LetterSacrificedEvent
    {
        public char Letter { get; set; }
        public int TileIndex { get; set; }
    }

    /// <summary>Fired when sacrifice mode is toggled on/off.</summary>
    public class SacrificeModeToggledEvent
    {
        public bool Active { get; set; }
    }

    /// <summary>Fired by Tome effects to request board reveals via EncounterManager.</summary>
    public class TomeRevealRequestEvent
    {
        public RevealType Type { get; set; }
        public List<char> Letters { get; set; }
        public int Count { get; set; }
    }

    // ── Intel (Gold-for-Intel Economy) ──────────────────────

    /// <summary>Type of purchasable intel on the map dossier.</summary>
    public enum IntelType
    {
        WordLength,
        FirstLetter,
        Weakness
    }

    /// <summary>Preview intel data for a single map node, generated at wave start.</summary>
    public class NodeIntelData
    {
        public int NodeIndex { get; set; }
        public int WordLength { get; set; }
        public char FirstLetter { get; set; }
        public string WeaknessHint { get; set; }
        public HashSet<IntelType> Unlocked { get; set; } = new();
    }

    /// <summary>Fired when the player spends gold to unlock a dossier intel line.</summary>
    public class IntelUnlockedEvent
    {
        public int NodeIndex { get; set; }
        public IntelType Type { get; set; }
        public int GoldSpent { get; set; }
    }

    // ── Wager (Pre-Encounter Gold Staking) ──────────────

    /// <summary>Fired when the player confirms a gold wager before an encounter.</summary>
    public class WagerConfirmedEvent
    {
        public int GoldStaked { get; set; }
        public float RewardMultiplier { get; set; }
        public int DamageBonus { get; set; }
    }

    // ── Crucible (Tome Fusion) ──────────────────────────

    /// <summary>Fired when two Tomes are fused in the crucible.</summary>
    public class CrucibleFusedEvent
    {
        public TomeInstance InputA { get; set; }
        public TomeInstance InputB { get; set; }
        public TomeInstance Result { get; set; }
    }
}
