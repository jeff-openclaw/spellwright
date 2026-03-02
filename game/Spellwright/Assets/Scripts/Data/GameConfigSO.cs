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
        public int baseScorePerCorrectGuess = 100;
        public int maxBossClueWords = 3;

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

        [Header("Difficulty Progression")]
        [Tooltip("Difficulty for encounters 1-2 (min, max)")]
        public Vector2Int earlyDifficulty = new Vector2Int(1, 2);
        [Tooltip("Difficulty for encounters 3-4 (min, max)")]
        public Vector2Int midDifficulty = new Vector2Int(2, 3);
        [Tooltip("Difficulty for encounters 5-6 (min, max)")]
        public Vector2Int lateDifficulty = new Vector2Int(3, 4);
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
        /// Returns the gold reward for winning an encounter.
        /// </summary>
        public int CalculateGoldReward(int guessesRemaining)
        {
            return baseGoldReward + goldPerRemainingGuess * guessesRemaining;
        }
    }
}
