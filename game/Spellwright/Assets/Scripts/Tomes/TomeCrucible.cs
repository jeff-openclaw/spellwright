using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Tomes
{
    /// <summary>
    /// Handles Tome fusion (crucible) on the map screen. Sacrifice two Tomes
    /// to forge one upgraded Tome with the rarer effect and an upgraded rarity.
    /// Limited to one fusion per wave.
    /// </summary>
    public class TomeCrucible : MonoBehaviour
    {
        public static TomeCrucible Instance { get; private set; }

        private int _lastFusionWave = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Whether a crucible fusion is available this wave.</summary>
        public bool CanFuse
        {
            get
            {
                if (TomeManager.Instance == null) return false;
                if (TomeManager.Instance.TomeSystem.Count < 2) return false;
                int wave = Run.RunManager.Instance?.WaveNumber ?? 0;
                return wave != _lastFusionWave;
            }
        }

        /// <summary>
        /// Fuses two equipped Tomes by their IDs. Removes both, creates an upgraded Tome
        /// with the rarer effect and bumped rarity. Returns the new TomeInstance or null on failure.
        /// </summary>
        public TomeInstance FuseTomes(string tomeIdA, string tomeIdB)
        {
            if (!CanFuse) return null;
            if (tomeIdA == tomeIdB) return null;

            var tm = TomeManager.Instance;
            if (tm == null) return null;

            var equipped = tm.TomeSystem.GetEquippedTomes();
            TomeInstance tomeA = null, tomeB = null;
            foreach (var t in equipped)
            {
                if (t.TomeId == tomeIdA) tomeA = t;
                if (t.TomeId == tomeIdB) tomeB = t;
            }

            if (tomeA == null || tomeB == null) return null;

            // Determine result: pick the rarer tome's effect, upgrade rarity
            TomeInstance primary = tomeA.Rarity >= tomeB.Rarity ? tomeA : tomeB;
            TomeInstance secondary = primary == tomeA ? tomeB : tomeA;

            TomeRarity fusedRarity = (TomeRarity)Mathf.Min((int)primary.Rarity + 1, (int)TomeRarity.Legendary);
            string fusedName = $"{primary.TomeName} \u2020"; // † symbol for fused

            var fusedInstance = new TomeInstance
            {
                TomeId = $"fused_{primary.TomeId}_{secondary.TomeId}",
                TomeName = fusedName,
                Rarity = fusedRarity,
                Category = primary.Category,
                EffectClassName = primary.EffectClassName
            };

            // Remove both input tomes
            tm.UnequipTome(tomeIdA);
            tm.UnequipTome(tomeIdB);

            // Equip the fused tome
            var effect = tm.CreateEffect(primary.EffectClassName);
            if (effect != null)
                tm.TomeSystem.EquipTome(fusedInstance, effect);

            // Mark this wave as used
            _lastFusionWave = Run.RunManager.Instance?.WaveNumber ?? 0;

            Debug.Log($"[TomeCrucible] Fused: {tomeA.TomeName} + {tomeB.TomeName} = {fusedName} ({fusedRarity})");

            EventBus.Instance.Publish(new CrucibleFusedEvent
            {
                InputA = tomeA,
                InputB = tomeB,
                Result = fusedInstance
            });

            return fusedInstance;
        }
    }
}
