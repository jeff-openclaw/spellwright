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
        public static RunManager Instance { get; private set; }

        [SerializeField] private GameConfigSO gameConfig;

        private RunState _state = new RunState();

        // ── Properties ───────────────────────────────────────

        public bool IsRunActive => _state.IsRunActive;
        public int CurrentHP => _state.CurrentHP;
        public int MaxHP => _state.MaxHP;
        public int Score => _state.Score;
        public int Gold => _state.Gold;
        public IReadOnlyList<string> UsedWords => _state.UsedWords;
        public int CurrentNodeIndex => _state.CurrentNodeIndex;

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

            // Generate a simple node sequence based on config
            _state.NodeSequence.Clear();
            if (gameConfig != null)
            {
                int totalFloors = gameConfig.actsPerRun * gameConfig.floorsPerAct;
                for (int i = 0; i < totalFloors; i++)
                {
                    // Last node of each act is a Boss, rest are Encounters
                    bool isActEnd = (i + 1) % gameConfig.floorsPerAct == 0;
                    _state.NodeSequence.Add(isActEnd ? NodeType.Boss : NodeType.Encounter);
                }
            }

            Debug.Log($"[RunManager] Run started — HP: {_state.CurrentHP}/{_state.MaxHP}, Nodes: {_state.NodeSequence.Count}");

            EventBus.Instance.Publish(new RunStartedEvent { State = _state });
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

            // Track used words
            if (!string.IsNullOrEmpty(evt.TargetWord))
                _state.UsedWords.Add(evt.TargetWord);

            Debug.Log($"[RunManager] Encounter ended — Score: +{evt.Score} (total: {_state.Score}), Used words: {_state.UsedWords.Count}");

            EventBus.Instance.Publish(new RunStateChangedEvent { State = _state });
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            if (!_state.IsRunActive) return;

            _state.CurrentHP = evt.NewHP;
            _state.MaxHP = evt.MaxHP;

            // End run if HP depleted
            if (_state.CurrentHP <= 0)
            {
                Debug.Log("[RunManager] HP depleted — ending run.");
                EndRun(won: false);
            }
        }
    }
}
