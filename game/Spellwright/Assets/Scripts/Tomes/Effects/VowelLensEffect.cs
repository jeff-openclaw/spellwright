using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes.Effects
{
    /// <summary>
    /// On wrong guess, reveals which positions in the target word contain vowels.
    /// </summary>
    public class VowelLensEffect : ITomeEffect
    {
        public string DisplayName => "Vowel Lens";

        private string _targetWord;
        private static readonly HashSet<char> Vowels = new HashSet<char> { 'a', 'e', 'i', 'o', 'u' };

        public void OnEncounterStart(EncounterStartedEvent evt)
        {
            _targetWord = evt.TargetWord?.ToLowerInvariant();
        }

        public void OnWrongGuess(GuessSubmittedEvent evt)
        {
            if (string.IsNullOrEmpty(_targetWord)) return;

            var positions = new List<int>();
            for (int i = 0; i < _targetWord.Length; i++)
            {
                if (Vowels.Contains(_targetWord[i]))
                    positions.Add(i + 1); // 1-based for display
            }

            string info = positions.Count > 0
                ? $"Vowels at positions: {string.Join(", ", positions)}"
                : "No vowels in the target word";

            EventBus.Instance.Publish(new TomeTriggeredEvent
            {
                TomeName = DisplayName,
                Description = "Reveals vowel positions in the target word",
                RevealedInfo = info
            });
        }

        public void OnCorrectGuess(GuessSubmittedEvent evt) { }
        public string OnClueReceived(ClueReceivedEvent evt) => null;
    }
}
