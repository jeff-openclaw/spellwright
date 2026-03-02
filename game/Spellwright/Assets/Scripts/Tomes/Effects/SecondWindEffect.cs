using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes.Effects
{
    /// <summary>
    /// Once per encounter, negates HP loss from the first wrong guess
    /// by setting a large PendingHPLossReduction on TomeSystem.
    /// </summary>
    public class SecondWindEffect : ITomeEffect
    {
        public string DisplayName => "Second Wind";

        private readonly TomeSystem _tomeSystem;
        private bool _usedThisEncounter;

        public SecondWindEffect(TomeSystem tomeSystem)
        {
            _tomeSystem = tomeSystem;
        }

        public void OnEncounterStart(EncounterStartedEvent evt)
        {
            _usedThisEncounter = false;
        }

        public void OnWrongGuess(GuessSubmittedEvent evt)
        {
            if (_usedThisEncounter) return;

            _usedThisEncounter = true;
            _tomeSystem.PendingHPLossReduction += 9999;

            EventBus.Instance.Publish(new TomeTriggeredEvent
            {
                TomeName = DisplayName,
                Description = "Negates HP loss from the first wrong guess",
                RevealedInfo = "Second Wind activated! No HP lost this time."
            });
        }

        public void OnCorrectGuess(GuessSubmittedEvent evt) { }
        public string OnClueReceived(ClueReceivedEvent evt) => null;
    }
}
