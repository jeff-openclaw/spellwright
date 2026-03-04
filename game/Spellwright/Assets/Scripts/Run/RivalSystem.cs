using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Run
{
    /// <summary>
    /// Designates one NPC as the player's Rival at run start and tracks
    /// rival encounter count for escalating AI prompts and bonus rewards.
    /// </summary>
    public class RivalSystem : MonoBehaviour
    {
        public static RivalSystem Instance { get; private set; }

        private NPCArchetype _rivalArchetype;
        private string _rivalDisplayName;
        private int _rivalEncounterCount;
        private bool _hasRival;

        public bool HasRival => _hasRival;
        public NPCArchetype RivalArchetype => _rivalArchetype;
        public string RivalDisplayName => _rivalDisplayName;
        public int RivalEncounterCount => _rivalEncounterCount;

        /// <summary>
        /// Rival tier: 1=Dismissive, 2=Engaged, 3+=Desperate.
        /// Returns 0 if no rival encounters yet.
        /// </summary>
        public int RivalTier => _rivalEncounterCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnRunStarted(RunStartedEvent evt)
        {
            // Pick a random non-boss, non-guide archetype as the rival
            var candidates = new[] { NPCArchetype.Riddlemaster, NPCArchetype.TricksterMerchant, NPCArchetype.SilentLibrarian };
            _rivalArchetype = candidates[Random.Range(0, candidates.Length)];
            _rivalEncounterCount = 0;
            _hasRival = true;

            // We don't know display name yet — it will be set on first encounter
            _rivalDisplayName = _rivalArchetype.ToString();

            Debug.Log($"[RivalSystem] Rival designated: {_rivalArchetype}");
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            if (!_hasRival || evt.NPC == null) return;

            if (evt.NPC.Archetype == _rivalArchetype)
            {
                _rivalEncounterCount++;
                _rivalDisplayName = evt.NPC.DisplayName;

                Debug.Log($"[RivalSystem] Rival encounter #{_rivalEncounterCount} (Tier {RivalTier}): {_rivalDisplayName}");

                _currentEncounterIsRival = true;

                EventBus.Instance.Publish(new RivalEncounterStartedEvent
                {
                    Tier = RivalTier
                });
            }
            else
            {
                _currentEncounterIsRival = false;
            }
        }

        private bool _currentEncounterIsRival;

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            // Check if this was a rival encounter (the last started encounter)
            if (_currentEncounterIsRival && evt.Won)
            {
                EventBus.Instance.Publish(new RivalDefeatedEvent
                {
                    Tier = RivalTier
                });
            }
            _currentEncounterIsRival = false;
        }

        /// <summary>
        /// Checks if a given NPC archetype matches the rival.
        /// Called by EncounterStartedEvent handler to set the rival flag.
        /// </summary>
        public bool IsRival(NPCArchetype archetype)
        {
            return _hasRival && archetype == _rivalArchetype;
        }
    }
}
