using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes.Effects
{
    /// <summary>
    /// On encounter start, reveals the first letter of the target word.
    /// </summary>
    public class FirstLightEffect : ITomeEffect
    {
        public string DisplayName => "First Light";

        public void OnEncounterStart(EncounterStartedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.TargetWord)) return;

            string firstLetter = evt.TargetWord.Substring(0, 1).ToUpperInvariant();

            EventBus.Instance.Publish(new TomeTriggeredEvent
            {
                TomeName = DisplayName,
                Description = "Reveals the first letter of the target word",
                RevealedInfo = $"The word starts with: {firstLetter}"
            });
        }

        public void OnWrongGuess(GuessSubmittedEvent evt) { }
        public void OnCorrectGuess(GuessSubmittedEvent evt) { }
        public string OnClueReceived(ClueReceivedEvent evt) => null;
    }
}
