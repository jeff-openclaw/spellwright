using UnityEngine;
using Spellwright.Data;

namespace Spellwright.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewNPC", menuName = "Spellwright/NPC Data")]
    public class NPCDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;
        public NPCArchetype archetype;

        [Header("Prompt")]
        [Tooltip("The .md file containing this NPC's system prompt")]
        public TextAsset systemPromptAsset;

        [Header("Behavior")]
        [Range(0.5f, 2.0f)]
        public float difficultyModifier = 1.0f;
        public bool isBoss;
        [TextArea(1, 3)]
        [Tooltip("Boss-only constraint, e.g. 'Your clues must be exactly 3 words.'")]
        public string bossConstraint;

        public NPCPromptData ToPromptData()
        {
            return new NPCPromptData
            {
                DisplayName = displayName,
                Archetype = archetype,
                SystemPromptTemplate = systemPromptAsset != null ? systemPromptAsset.text : "",
                DifficultyModifier = difficultyModifier,
                IsBoss = isBoss,
                BossConstraint = bossConstraint
            };
        }
    }
}
