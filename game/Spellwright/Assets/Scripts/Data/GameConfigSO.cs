using Spellwright.Data;
using UnityEngine;

namespace Spellwright.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Spellwright/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Language")]
        public GameLanguage language = GameLanguage.English;

        /// <summary>Returns the Hunspell language code for the selected language.</summary>
        public string HunspellLanguageCode => language switch
        {
            GameLanguage.Romanian => "ro_RO",
            _ => "en_US"
        };

        [Header("Player Defaults")]
        public int startingHP = 30;
        public int startingGold = 0;
        public int maxTomeSlots = 5;

        [Header("Encounter")]
        public int maxGuessesPerEncounter = 6;
        public int hpLostPerWrongGuess = 4;
        public int hpLostPerWrongLetter = 10;
        public int hpLostPerWrongPhrase = 10;
        [Tooltip("HP cost when guessing a letter correctly (buying information has a price)")]
        public int hpCostPerCorrectLetter = 5;
        public int baseScorePerCorrectGuess = 100;
        public int maxBossClueWords = 3;
        [Tooltip("Number of random letters revealed per clue received")]
        public int lettersRevealedPerClue = 1;
        [Tooltip("Reveal a random letter as consolation when a letter guess misses")]
        public bool consolationRevealOnWrongLetter = true;

        [Header("Economy")]
        [Tooltip("Base gold reward for winning an encounter")]
        public int baseGoldReward = 8;
        [Tooltip("Extra gold per remaining guess when winning")]
        public int goldPerRemainingGuess = 2;
        [Tooltip("Tome price multiplier applied to shopCost (lower = cheaper tomes)")]
        [Range(0.1f, 2f)]
        public float tomePriceMultiplier = 0.3f;
        [Tooltip("Heal cost in the shop")]
        public int healCost = 8;
        [Tooltip("HP restored by heal")]
        public int healAmount = 8;

        [Header("Intel Costs (Gold-for-Intel)")]
        [Tooltip("Cost to unlock word length intel (easy/mid/hard)")]
        public Vector3Int intelCostWordLength = new Vector3Int(3, 5, 8);
        [Tooltip("Cost to unlock first letter intel (easy/mid/hard)")]
        public Vector3Int intelCostFirstLetter = new Vector3Int(5, 8, 12);
        [Tooltip("Cost to unlock NPC weakness intel (easy/mid/hard)")]
        public Vector3Int intelCostWeakness = new Vector3Int(8, 10, 15);

        [Header("Difficulty Progression")]
        [Tooltip("Difficulty for encounters 1-2 (min, max)")]
        public Vector2Int earlyDifficulty = new Vector2Int(1, 2);
        [Tooltip("Difficulty for encounters 3-4 (min, max)")]
        public Vector2Int midDifficulty = new Vector2Int(2, 4);
        [Tooltip("Difficulty for encounters 5-6 (min, max)")]
        public Vector2Int lateDifficulty = new Vector2Int(3, 5);
        [Tooltip("Boss difficulty (min, max)")]
        public Vector2Int bossDifficulty = new Vector2Int(3, 4);

        [Header("LLM")]
        [Tooltip("GGUF model filename in StreamingAssets/Models/")]
        public string modelFileName = "llama-3.2-3b-q4_k_m.gguf";
        public int maxTokens = 256;
        [Range(0f, 2f)]
        public float temperature = 0.7f;

        [Header("Run Structure")]
        public int actsPerRun = 3;
        public int floorsPerAct = 3;

        /// <summary>
        /// Returns the difficulty range for the given encounter number (1-based).
        /// </summary>
        public Vector2Int GetDifficultyForEncounter(int encounterNumber)
        {
            if (encounterNumber <= 2) return earlyDifficulty;
            if (encounterNumber <= 4) return midDifficulty;
            return lateDifficulty;
        }

        /// <summary>
        /// Returns the intel cost for the given type and encounter number.
        /// Difficulty tier: encounters 1-2 = easy, 3-4 = mid, 5+ = hard.
        /// </summary>
        public int GetIntelCost(IntelType type, int encounterNumber)
        {
            int tier = encounterNumber <= 2 ? 0 : encounterNumber <= 4 ? 1 : 2;
            return type switch
            {
                IntelType.WordLength => intelCostWordLength[tier],
                IntelType.FirstLetter => intelCostFirstLetter[tier],
                IntelType.Weakness => intelCostWeakness[tier],
                _ => 5
            };
        }

        /// <summary>
        /// Returns the gold reward for winning an encounter.
        /// </summary>
        public int CalculateGoldReward(int guessesRemaining)
        {
            return baseGoldReward + goldPerRemainingGuess * guessesRemaining;
        }
    }
}
