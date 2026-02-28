using System;
using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;

namespace Spellwright.Tomes
{
    /// <summary>
    /// Hook interface for Tome effects. Each Tome implements this to
    /// react to encounter lifecycle events.
    /// </summary>
    public interface ITomeEffect
    {
        /// <summary>Display name shown in UI and passed to the prompt builder.</summary>
        string DisplayName { get; }

        /// <summary>Called when an encounter begins.</summary>
        void OnEncounterStart(EncounterStartedEvent evt);

        /// <summary>Called when the player guesses incorrectly.</summary>
        void OnWrongGuess(GuessSubmittedEvent evt);

        /// <summary>Called when the player guesses correctly.</summary>
        void OnCorrectGuess(GuessSubmittedEvent evt);

        /// <summary>
        /// Called when a clue is received from the LLM.
        /// Effects may modify the clue text via the return value.
        /// </summary>
        /// <param name="evt">The clue event.</param>
        /// <returns>Optionally modified clue text, or null to leave unchanged.</returns>
        string OnClueReceived(ClueReceivedEvent evt);
    }

    /// <summary>
    /// Manages equipped Tomes (max <see cref="MaxSlots"/>) and dispatches
    /// encounter events to active <see cref="ITomeEffect"/> instances.
    /// </summary>
    public class TomeSystem
    {
        /// <summary>Maximum number of Tome slots.</summary>
        public const int MaxSlots = 5;

        private readonly List<EquippedTome> _equipped = new List<EquippedTome>();
        private readonly EventBus _eventBus;

        public TomeSystem(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            _eventBus.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            _eventBus.Subscribe<ClueReceivedEvent>(OnClueReceived);
        }

        /// <summary>Number of currently equipped Tomes.</summary>
        public int Count => _equipped.Count;

        /// <summary>Whether a new Tome can be equipped.</summary>
        public bool HasFreeSlot => _equipped.Count < MaxSlots;

        /// <summary>
        /// Equips a Tome. Returns false if slots are full or the Tome is already equipped.
        /// </summary>
        public bool EquipTome(TomeInstance tome, ITomeEffect effect)
        {
            if (tome == null || effect == null) return false;
            if (_equipped.Count >= MaxSlots) return false;
            if (_equipped.Exists(e => e.Instance.TomeId == tome.TomeId)) return false;

            _equipped.Add(new EquippedTome { Instance = tome, Effect = effect });

            _eventBus.Publish(new TomeEquippedEvent
            {
                Tome = tome,
                SlotIndex = _equipped.Count - 1
            });

            return true;
        }

        /// <summary>Removes a Tome by its ID. Returns false if not found.</summary>
        public bool UnequipTome(string tomeId)
        {
            var idx = _equipped.FindIndex(e => e.Instance.TomeId == tomeId);
            if (idx < 0) return false;

            var removed = _equipped[idx];
            _equipped.RemoveAt(idx);

            _eventBus.Publish(new TomeRemovedEvent { Tome = removed.Instance });

            return true;
        }

        /// <summary>Returns display names of all active Tome effects (for prompt building).</summary>
        public List<string> GetActiveTomeEffectNames()
        {
            var names = new List<string>(_equipped.Count);
            foreach (var t in _equipped)
                names.Add(t.Effect.DisplayName);
            return names;
        }

        /// <summary>Returns a read-only snapshot of equipped Tome instances.</summary>
        public IReadOnlyList<TomeInstance> GetEquippedTomes()
        {
            var list = new List<TomeInstance>(_equipped.Count);
            foreach (var t in _equipped)
                list.Add(t.Instance);
            return list;
        }

        // ── Event Handlers ─────────────────────────────────

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            foreach (var t in _equipped)
                t.Effect.OnEncounterStart(evt);
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            if (evt.Result == null) return;

            foreach (var t in _equipped)
            {
                if (evt.Result.IsCorrect)
                    t.Effect.OnCorrectGuess(evt);
                else
                    t.Effect.OnWrongGuess(evt);
            }
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            foreach (var t in _equipped)
            {
                var modified = t.Effect.OnClueReceived(evt);
                if (modified != null && evt.Clue != null)
                    evt.Clue.Clue = modified;
            }
        }

        /// <summary>
        /// Unsubscribes from the event bus. Call when the TomeSystem is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            _eventBus.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            _eventBus.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
        }

        // ── Internal ──────────────────────────────────────

        private class EquippedTome
        {
            public TomeInstance Instance;
            public ITomeEffect Effect;
        }
    }
}
