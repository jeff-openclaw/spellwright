using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;

namespace Spellwright.Run
{
    /// <summary>
    /// Singleton MonoBehaviour that owns run-level state (HP, score, gold, used words).
    /// Persists across scenes. Subscribes to encounter events to accumulate state.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; set; }

        [SerializeField] private GameConfigSO gameConfig;

        private RunState _state = new RunState();
        private int _waveNumber;

        // ── Properties ───────────────────────────────────────

        public bool IsRunActive => _state.IsRunActive;
        public int CurrentHP => _state.CurrentHP;
        public int MaxHP => _state.MaxHP;
        public int Score => _state.Score;
        public int Gold => _state.Gold;
        public IReadOnlyList<string> UsedWords => _state.UsedWords;
        public int CurrentNodeIndex => _state.CurrentNodeIndex;
        public IReadOnlyList<NodeType> NodeSequence => _state.NodeSequence;
        public int WaveNumber => _waveNumber;

        /// <summary>The NodeType at the current index, or Encounter if out of range.</summary>
        public NodeType CurrentNodeType =>
            _state.CurrentNodeIndex < _state.NodeSequence.Count
                ? _state.NodeSequence[_state.CurrentNodeIndex]
                : NodeType.Encounter;

        /// <summary>Total number of encounters won in this run.</summary>
        public int EncountersWon { get; private set; }

        // ── Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Initializes a new run from GameConfig defaults.
        /// </summary>
        public void StartRun()
        {
            _state = new RunState
            {
                CurrentHP = gameConfig != null ? gameConfig.startingHP : 100,
                MaxHP = gameConfig != null ? gameConfig.startingHP : 100,
                Gold = gameConfig != null ? gameConfig.startingGold : 0,
                Score = 0,
                CurrentNodeIndex = 0,
                IsRunActive = true
            };

            EncountersWon = 0;
            _waveNumber = 1;

            // Generate wave 1: (E-S)×5 + B
            _state.NodeSequence.Clear();
            AppendWaveNodes();

            Debug.Log($"[RunManager] Run started — HP: {_state.CurrentHP}/{_state.MaxHP}, Wave: {_waveNumber}, Nodes: {_state.NodeSequence.Count}");

            EventBus.Instance.Publish(new RunStartedEvent { State = _state });
        }

        /// <summary>
        /// Starts the next wave after a boss victory. Appends new nodes and publishes events.
        /// </summary>
        public void StartNextWave()
        {
            if (!_state.IsRunActive) return;

            _waveNumber++;
            AppendWaveNodes();

            Debug.Log($"[RunManager] Wave {_waveNumber} started — Nodes: {_state.NodeSequence.Count}");

            EventBus.Instance.Publish(new RunStartedEvent { State = _state });
        }

        /// <summary>Appends one wave of nodes: E×5 + B. Shop is shown post-encounter, not as a map node.</summary>
        private void AppendWaveNodes()
        {
            for (int i = 0; i < 5; i++)
                _state.NodeSequence.Add(NodeType.Encounter);
            _state.NodeSequence.Add(NodeType.Boss);
        }

        /// <summary>
        /// Ends the current run.
        /// </summary>
        public void EndRun(bool won = false)
        {
            if (!_state.IsRunActive) return;

            _state.IsRunActive = false;

            Debug.Log($"[RunManager] Run ended — Score: {_state.Score}, Won: {won}");

            EventBus.Instance.Publish(new RunEndedEvent
            {
                FinalScore = _state.Score,
                Won = won
            });
        }

        /// <summary>
        /// Advances to the next node in the sequence.
        /// </summary>
        public void AdvanceNode()
        {
            if (!_state.IsRunActive) return;

            _state.CurrentNodeIndex++;

            if (_state.CurrentNodeIndex >= _state.NodeSequence.Count)
            {
                EndRun(won: true);
                return;
            }

            EventBus.Instance.Publish(new RunStateChangedEvent { State = _state });
        }

        // ── Event Handlers ───────────────────────────────────

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (!_state.IsRunActive) return;

            // Accumulate score
            _state.Score += evt.Score;

            // Track encounters won
            if (evt.Won) EncountersWon++;

            // Gold reward for winning — config-driven
            if (evt.Won)
            {
                int guessesRemaining = gameConfig != null
                    ? gameConfig.maxGuessesPerEncounter - evt.GuessCount
                    : 6 - evt.GuessCount;
                int reward = gameConfig != null
                    ? gameConfig.CalculateGoldReward(guessesRemaining)
                    : 8 + 2 * guessesRemaining;
                AddGold(reward);
            }

            // Track used words
            if (!string.IsNullOrEmpty(evt.TargetWord))
                _state.UsedWords.Add(evt.TargetWord);

            Debug.Log($"[RunManager] Encounter ended — Score: +{evt.Score} (total: {_state.Score}), Used words: {_state.UsedWords.Count}");

            EventBus.Instance.Publish(new RunStateChangedEvent { State = _state });
        }

        /// <summary>Adds gold and publishes a GoldChangedEvent.</summary>
        public void AddGold(int amount)
        {
            if (amount == 0) return;
            int old = _state.Gold;
            _state.Gold += amount;
            EventBus.Instance.Publish(new GoldChangedEvent { OldGold = old, NewGold = _state.Gold });
        }

        /// <summary>Spends gold if sufficient. Returns true on success.</summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || _state.Gold < amount) return false;
            int old = _state.Gold;
            _state.Gold -= amount;
            EventBus.Instance.Publish(new GoldChangedEvent { OldGold = old, NewGold = _state.Gold });
            return true;
        }

        /// <summary>Heals the player by the given amount (capped at MaxHP).</summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            int old = _state.CurrentHP;
            _state.CurrentHP = Mathf.Min(_state.CurrentHP + amount, _state.MaxHP);
            if (_state.CurrentHP != old)
            {
                _publishingHP = true;
                EventBus.Instance.Publish(new HPChangedEvent
                {
                    OldHP = old,
                    NewHP = _state.CurrentHP,
                    MaxHP = _state.MaxHP
                });
                _publishingHP = false;
            }
        }

        private bool _publishingHP;

        /// <summary>Applies damage to the player. Used by EncounterManager indirectly via events,
        /// and directly for testing.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !_state.IsRunActive) return;
            int old = _state.CurrentHP;
            _state.CurrentHP = Mathf.Max(0, _state.CurrentHP - amount);

            _publishingHP = true;
            EventBus.Instance.Publish(new HPChangedEvent
            {
                OldHP = old,
                NewHP = _state.CurrentHP,
                MaxHP = _state.MaxHP
            });
            _publishingHP = false;

            if (_state.CurrentHP <= 0)
            {
                Debug.Log("[RunManager] HP depleted — ending run.");
                EndRun(won: false);
            }
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            // Ignore events we published ourselves (TakeDamage / Heal)
            if (_publishingHP || !_state.IsRunActive) return;

            _state.CurrentHP = evt.NewHP;
            _state.MaxHP = evt.MaxHP;

            // End run if HP depleted (from EncounterManager's HP tracking)
            if (_state.CurrentHP <= 0)
            {
                Debug.Log("[RunManager] HP depleted — ending run.");
                EndRun(won: false);
            }
        }
    }
}
