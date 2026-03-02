using System.Collections.Generic;
using System.Linq;
using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes.Effects
{
    /// <summary>
    /// On wrong guess, shows which letters from the guess also appear in the target word.
    /// </summary>
    public class EchoChamberEffect : ITomeEffect
    {
        public string DisplayName => "Echo Chamber";

        private HashSet<char> _targetLetters;

        public void OnEncounterStart(EncounterStartedEvent evt)
        {
            _targetLetters = string.IsNullOrEmpty(evt.TargetWord)
                ? new HashSet<char>()
                : new HashSet<char>(evt.TargetWord.ToLowerInvariant());
        }

        public void OnWrongGuess(GuessSubmittedEvent evt)
        {
            if (_targetLetters == null || _targetLetters.Count == 0) return;
            if (string.IsNullOrEmpty(evt.Guess)) return;

            var guessLetters = new HashSet<char>(evt.Guess.ToLowerInvariant());
            var overlap = guessLetters.Where(c => _targetLetters.Contains(c)).OrderBy(c => c).ToList();

            string info = overlap.Count > 0
                ? $"Shared letters: {string.Join(", ", overlap.Select(c => c.ToString().ToUpperInvariant()))}"
                : "No shared letters";

            EventBus.Instance.Publish(new TomeTriggeredEvent
            {
                TomeName = DisplayName,
                Description = "Shows letter overlap between guess and answer",
                RevealedInfo = info
            });
        }

        public void OnCorrectGuess(GuessSubmittedEvent evt) { }
        public string OnClueReceived(ClueReceivedEvent evt) => null;
    }
}
