using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Reads the NPC mood from each clue and classifies it into a difficulty shift
    /// (Mercy / Normal / Cruel). Publishes DifficultyShiftChangedEvent so that
    /// EncounterManager adjusts letter reveals and PromptBuilder adjusts clue difficulty.
    /// </summary>
    public class AdaptiveDifficultyMod : MonoBehaviour
    {
        public DifficultyShift CurrentShift { get; private set; } = DifficultyShift.Normal;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            CurrentShift = DifficultyShift.Normal;
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            var mood = evt.Clue?.Mood?.ToLowerInvariant()?.Trim() ?? "neutral";
            var newShift = ClassifyMood(mood);

            if (newShift != CurrentShift)
            {
                CurrentShift = newShift;
                Debug.Log($"[AdaptiveDifficulty] Mood '{mood}' → Shift: {CurrentShift}");

                EventBus.Instance.Publish(new DifficultyShiftChangedEvent
                {
                    Shift = CurrentShift
                });
            }
        }

        private static DifficultyShift ClassifyMood(string mood)
        {
            return mood switch
            {
                "frustrated" or "encouraging" => DifficultyShift.Mercy,
                "amused" or "taunting" or "menacing" => DifficultyShift.Cruel,
                _ => DifficultyShift.Normal
            };
        }
    }
}
