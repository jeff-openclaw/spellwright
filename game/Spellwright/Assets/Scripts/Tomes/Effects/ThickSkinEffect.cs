using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes.Effects
{
    /// <summary>
    /// On encounter start, grants +10 max HP bonus via TomeSystem's pending modifier.
    /// </summary>
    public class ThickSkinEffect : ITomeEffect
    {
        public string DisplayName => "Thick Skin";

        private readonly TomeSystem _tomeSystem;

        public ThickSkinEffect(TomeSystem tomeSystem)
        {
            _tomeSystem = tomeSystem;
        }

        public void OnEncounterStart(EncounterStartedEvent evt)
        {
            _tomeSystem.PendingMaxHPBonus += 10;

            EventBus.Instance.Publish(new TomeTriggeredEvent
            {
                TomeName = DisplayName,
                Description = "Grants +10 max HP at encounter start",
                RevealedInfo = "+10 max HP"
            });
        }

        public void OnWrongGuess(GuessSubmittedEvent evt) { }
        public void OnCorrectGuess(GuessSubmittedEvent evt) { }
        public string OnClueReceived(ClueReceivedEvent evt) => null;
    }
}
