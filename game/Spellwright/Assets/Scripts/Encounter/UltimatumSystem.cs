using Spellwright.Core;
using Spellwright.Data;
using Spellwright.LLM;
using Spellwright.Rendering;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Triggers the ultimatum sequence when the player reaches their final guess.
    /// Dims the screen, activates CRT clean mode, requests a dramatic NPC line,
    /// and starts a 15-second countdown. Resolves on guess or timeout.
    /// </summary>
    public class UltimatumSystem : MonoBehaviour
    {
        [SerializeField] private float countdownSeconds = 15f;

        private bool _ultimatumActive;
        private float _timeRemaining;
        private EncounterManager _encounter;
        private NPCPromptData _npcData;
        private string _lastMood = "neutral";
        private string _targetWord;

        public bool IsActive => _ultimatumActive;
        public float TimeRemaining => _timeRemaining;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            RestoreCRT();
        }

        private void Update()
        {
            if (!_ultimatumActive) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _ultimatumActive = false;
                RestoreCRT();
                EventBus.Instance.Publish(new UltimatumExpiredEvent());
            }
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            _ultimatumActive = false;
            _npcData = evt.NPC;
            _targetWord = evt.TargetWord;
            _lastMood = "neutral";
            _encounter = FindAnyObjectByType<EncounterManager>();
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            _lastMood = evt.Clue?.Mood ?? "neutral";
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            if (_ultimatumActive)
            {
                // Player submitted during ultimatum — resolve normally, restore CRT
                _ultimatumActive = false;
                RestoreCRT();
                return;
            }

            // Check if next guess will be the last
            if (_encounter == null) return;
            if (_encounter.GuessesRemaining == 1 && !_ultimatumActive)
            {
                TriggerUltimatum();
            }
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (_ultimatumActive)
            {
                _ultimatumActive = false;
                RestoreCRT();
            }
        }

        private async void TriggerUltimatum()
        {
            _ultimatumActive = true;
            _timeRemaining = countdownSeconds;

            // Activate CRT clean mode
            if (CRTSettings.Instance != null)
                CRTSettings.Instance.SetCleanMode(true);

            EventBus.Instance.Publish(new UltimatumTriggeredEvent
            {
                NpcName = _npcData?.DisplayName ?? "???",
                Mood = _lastMood
            });

            // Request dramatic NPC line from LLM
            if (LLMManager.Instance != null && _npcData != null)
            {
                string line = await LLMManager.Instance.GenerateUltimatumLineAsync(
                    _npcData, _lastMood, _targetWord);

                if (_ultimatumActive) // Still active after async
                {
                    EventBus.Instance.Publish(new UltimatumLineReceivedEvent { Line = line });
                }
            }
        }

        private void RestoreCRT()
        {
            if (CRTSettings.Instance != null && CRTSettings.Instance.IsCleanMode)
                CRTSettings.Instance.SetCleanMode(false);
        }
    }
}
