using UnityEngine;

namespace Spellwright.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Spellwright/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player Defaults")]
        public int startingHP = 100;
        public int startingGold = 0;
        public int maxTomeSlots = 5;

        [Header("Encounter")]
        public int maxGuessesPerEncounter = 6;
        public int hpLostPerWrongGuess = 15;
        public int baseScorePerCorrectGuess = 100;

        [Header("LLM")]
        [Tooltip("GGUF model filename in StreamingAssets/Models/")]
        public string modelFileName = "llama-3.2-3b-q4_k_m.gguf";
        public int maxTokens = 256;
        [Range(0f, 2f)]
        public float temperature = 0.7f;

        [Header("Run Structure")]
        public int actsPerRun = 3;
        public int floorsPerAct = 3;
    }
}
