using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Offers time-limited bargains when the NPC's mood shifts during an encounter.
    /// Subscribes to ClueReceivedEvent, detects mood changes, selects a deal based
    /// on NPC archetype + mood, and manages the 8-second accept window.
    /// </summary>
    public class MoodBargainSystem : MonoBehaviour
    {
        [SerializeField] private float bargainDurationSeconds = 8f;

        private string _lastMood;
        private bool _bargainActive;
        private float _timeRemaining;
        private BargainEffect _currentEffect;
        private NPCArchetype _currentArchetype;
        private EncounterManager _encounter;

        // Tracks double-risk state for next wrong guess
        private bool _doubleRiskActive;
        public bool DoubleRiskActive => _doubleRiskActive;
        public void ClearDoubleRisk() => _doubleRiskActive = false;

        private static readonly Dictionary<(NPCArchetype, string), BargainEntry> BargainTable = new()
        {
            // Guide — generous
            { (NPCArchetype.Guide, "frustrated"), new BargainEntry("Free vowel reveal", "", "Let me help — I'll show you a vowel.", BargainEffect.RevealVowel) },
            { (NPCArchetype.Guide, "encouraging"), new BargainEntry("Small heal", "", "You're doing well! Here, take a breather.", BargainEffect.HealSmall) },

            // Riddlemaster — fair trades
            { (NPCArchetype.Riddlemaster, "frustrated"), new BargainEntry("Vowel for a guess", "Costs 1 guess", "A vowel for a guess. Fair trade?", BargainEffect.RevealVowel) },
            { (NPCArchetype.Riddlemaster, "amused"), new BargainEntry("Double stakes", "2× HP risk on next miss", "Double the stakes?", BargainEffect.DoubleRisk) },

            // TricksterMerchant — risky
            { (NPCArchetype.TricksterMerchant, "frustrated"), new BargainEntry("A vowel... probably", "Costs 1 guess", "I'll show you a letter... probably the right one.", BargainEffect.RevealVowel) },
            { (NPCArchetype.TricksterMerchant, "amused"), new BargainEntry("Double stakes", "2× HP risk on next miss", "All or nothing? Your HP against my secret.", BargainEffect.DoubleRisk) },

            // SilentLibrarian — cryptic
            { (NPCArchetype.SilentLibrarian, "frustrated"), new BargainEntry("Whispered vowel", "", "...", BargainEffect.RevealVowel) },
            { (NPCArchetype.SilentLibrarian, "amused"), new BargainEntry("Skip ahead", "Costs 1 guess", "*slides a note*", BargainEffect.SkipGuess) },
        };

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<BargainAcceptedEvent>(OnBargainAccepted);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<BargainAcceptedEvent>(OnBargainAccepted);
        }

        private void Update()
        {
            if (!_bargainActive) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _bargainActive = false;
                EventBus.Instance.Publish(new BargainExpiredEvent());
                Debug.Log("[MoodBargainSystem] Bargain expired.");
            }
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            _lastMood = null;
            _bargainActive = false;
            _doubleRiskActive = false;
            _currentArchetype = evt.NPC != null ? evt.NPC.Archetype : NPCArchetype.Guide;
            _encounter = FindAnyObjectByType<EncounterManager>();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            _bargainActive = false;
            _doubleRiskActive = false;
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            if (_bargainActive) return; // Only one bargain at a time

            string mood = evt.Clue?.Mood;
            if (string.IsNullOrEmpty(mood)) return;

            // Only trigger on mood CHANGE
            bool moodChanged = _lastMood != null && mood != _lastMood;
            _lastMood = mood;

            if (!moodChanged) return;

            // Look up bargain for this archetype + mood
            if (!BargainTable.TryGetValue((_currentArchetype, mood), out var entry))
                return;

            _bargainActive = true;
            _timeRemaining = bargainDurationSeconds;
            _currentEffect = entry.Effect;

            Debug.Log($"[MoodBargainSystem] Offering bargain: {entry.Description} ({entry.Effect})");

            EventBus.Instance.Publish(new BargainOfferedEvent
            {
                Description = entry.Description,
                CostDescription = entry.CostDescription,
                NpcFlavorText = entry.FlavorText,
                Effect = entry.Effect,
                NpcName = "" // UI gets name from encounter
            });
        }

        private void OnBargainAccepted(BargainAcceptedEvent evt)
        {
            if (!_bargainActive) return;
            _bargainActive = false;

            Debug.Log($"[MoodBargainSystem] Bargain accepted: {evt.Effect}");

            switch (evt.Effect)
            {
                case BargainEffect.RevealVowel:
                    ApplyRevealVowel();
                    break;
                case BargainEffect.SkipGuess:
                    // EncounterManager handles guess consumption via its own BargainAcceptedEvent handler
                    break;
                case BargainEffect.DoubleRisk:
                    _doubleRiskActive = true;
                    break;
                case BargainEffect.HealSmall:
                    Run.RunManager.Instance?.Heal(2);
                    break;
            }
        }

        private void ApplyRevealVowel()
        {
            if (_encounter == null || _encounter.Board == null) return;
            int idx = _encounter.Board.RevealRandomVowel();
            if (idx >= 0)
            {
                char letter = _encounter.Board.Tiles[idx].Character;
                EventBus.Instance.Publish(new LetterRevealedEvent
                {
                    RevealedPositions = new List<int> { idx },
                    RevealedLetter = letter,
                    Source = "bargain"
                });
            }
        }

        public float TimeRemaining => _timeRemaining;
        public float Duration => bargainDurationSeconds;
        public bool IsBargainActive => _bargainActive;

        private struct BargainEntry
        {
            public string Description;
            public string CostDescription;
            public string FlavorText;
            public BargainEffect Effect;

            public BargainEntry(string desc, string cost, string flavor, BargainEffect effect)
            {
                Description = desc;
                CostDescription = cost;
                FlavorText = flavor;
                Effect = effect;
            }
        }
    }
}
