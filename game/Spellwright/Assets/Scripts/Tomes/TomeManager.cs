using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using Spellwright.Tomes.Effects;
using UnityEngine;

namespace Spellwright.Tomes
{
    /// <summary>
    /// Singleton MonoBehaviour that owns the TomeSystem and provides
    /// a high-level API for equipping/unequipping Tomes from TomeDataSO assets.
    /// </summary>
    public class TomeManager : MonoBehaviour
    {
        public static TomeManager Instance { get; set; }

        public TomeSystem TomeSystem { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            TomeSystem = new TomeSystem(EventBus.Instance);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                TomeSystem?.Dispose();
                Instance = null;
            }
        }

        /// <summary>
        /// Equips a Tome from a ScriptableObject asset. Creates the matching ITomeEffect
        /// via factory lookup on effectClassName.
        /// </summary>
        public bool EquipTome(TomeDataSO tomeData)
        {
            if (tomeData == null) return false;

            var instance = tomeData.ToInstance();
            var effect = CreateEffect(tomeData.effectClassName);
            if (effect == null)
            {
                Debug.LogWarning($"[TomeManager] Unknown effect class: \"{tomeData.effectClassName}\"");
                return false;
            }

            bool equipped = TomeSystem.EquipTome(instance, effect);
            if (equipped)
                Debug.Log($"[TomeManager] Equipped \"{tomeData.displayName}\" (effect: {tomeData.effectClassName})");
            return equipped;
        }

        /// <summary>Removes a Tome by its ID.</summary>
        public bool UnequipTome(string tomeId)
        {
            bool removed = TomeSystem.UnequipTome(tomeId);
            if (removed)
                Debug.Log($"[TomeManager] Unequipped tome \"{tomeId}\"");
            return removed;
        }

        /// <summary>Returns display names of all active effects (for LLM prompt building).</summary>
        public List<string> GetActiveEffectNames()
        {
            return TomeSystem.GetActiveTomeEffectNames();
        }

        /// <summary>
        /// Factory method that creates an ITomeEffect from a class name string.
        /// </summary>
        public ITomeEffect CreateEffect(string effectClassName)
        {
            return effectClassName switch
            {
                "VowelLensEffect" => new VowelLensEffect(),
                "FirstLightEffect" => new FirstLightEffect(),
                "EchoChamberEffect" => new EchoChamberEffect(),
                "ThickSkinEffect" => new ThickSkinEffect(TomeSystem),
                "SecondWindEffect" => new SecondWindEffect(TomeSystem),
                _ => null
            };
        }
    }
}
