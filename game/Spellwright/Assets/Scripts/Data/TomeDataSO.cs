using UnityEngine;
using Spellwright.Data;

namespace Spellwright.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewTome", menuName = "Spellwright/Tome Data")]
    public class TomeDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string tomeId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Classification")]
        public TomeRarity rarity;
        public TomeCategory category;

        [Header("Effect")]
        [Tooltip("The ITomeEffect class name (e.g. VowelLensEffect)")]
        public string effectClassName;

        [Header("Economy")]
        public int shopCost = 50;

        public TomeInstance ToInstance()
        {
            return new TomeInstance
            {
                TomeId = tomeId,
                TomeName = displayName,
                Rarity = rarity,
                Category = category,
                EffectClassName = effectClassName
            };
        }
    }
}
