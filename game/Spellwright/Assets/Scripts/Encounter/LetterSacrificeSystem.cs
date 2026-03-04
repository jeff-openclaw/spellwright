using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Allows the player to sacrifice a revealed letter to demand a better clue.
    /// Limited to one sacrifice per encounter. The NPC's personality shapes the
    /// quality of the sacrifice response via PromptBuilder.
    /// </summary>
    public class LetterSacrificeSystem : MonoBehaviour
    {
        private bool _sacrificeUsed;
        private bool _sacrificeModeActive;
        private EncounterManager _encounter;

        public bool SacrificeUsed => _sacrificeUsed;
        public bool SacrificeModeActive => _sacrificeModeActive;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            _sacrificeUsed = false;
            _sacrificeModeActive = false;
            _encounter = FindAnyObjectByType<EncounterManager>();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            _sacrificeModeActive = false;
        }

        /// <summary>Toggle sacrifice mode on/off.</summary>
        public void ToggleSacrificeMode()
        {
            if (_sacrificeUsed) return;

            _sacrificeModeActive = !_sacrificeModeActive;
            EventBus.Instance.Publish(new SacrificeModeToggledEvent { Active = _sacrificeModeActive });
            Debug.Log($"[LetterSacrificeSystem] Sacrifice mode: {(_sacrificeModeActive ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Called when a revealed tile is clicked in sacrifice mode.
        /// Re-hides the tile and triggers a sacrifice clue request.
        /// </summary>
        public void SacrificeTile(int tileIndex)
        {
            if (_sacrificeUsed || !_sacrificeModeActive) return;
            if (_encounter == null || _encounter.Board == null) return;

            char letter = _encounter.Board.RehideTile(tileIndex);
            if (letter == '\0') return; // Invalid tile

            _sacrificeUsed = true;
            _sacrificeModeActive = false;

            Debug.Log($"[LetterSacrificeSystem] Sacrificed letter '{letter}' at tile {tileIndex}");

            EventBus.Instance.Publish(new SacrificeModeToggledEvent { Active = false });
            EventBus.Instance.Publish(new LetterSacrificedEvent
            {
                Letter = letter,
                TileIndex = tileIndex
            });
        }
    }
}
