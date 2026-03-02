using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Run
{
    /// <summary>
    /// Central state machine managing the game flow:
    /// MainMenu → RunSetup → Map ↔ Encounter/Shop/Boss → RunEnd → MainMenu.
    /// Shows/hides UI panels based on state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject encounterPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject runEndPanel;

        private GameState _currentState = GameState.MainMenu;
        public GameState CurrentState => _currentState;

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
            EventBus.Instance.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            TransitionTo(GameState.MainMenu);
        }

        /// <summary>
        /// Transitions to a new state, hiding/showing the appropriate panels.
        /// </summary>
        public void TransitionTo(GameState newState)
        {
            var oldState = _currentState;
            _currentState = newState;

            HideAllPanels();

            switch (newState)
            {
                case GameState.MainMenu:
                    ShowPanel(mainMenuPanel);
                    break;

                case GameState.RunSetup:
                    // Start a new run, then go directly to Map
                    if (RunManager.Instance != null)
                        RunManager.Instance.StartRun();
                    TransitionTo(GameState.Map);
                    return;

                case GameState.Map:
                    ShowPanel(mapPanel);
                    break;

                case GameState.Encounter:
                case GameState.Boss:
                    ShowPanel(encounterPanel);
                    break;

                case GameState.Shop:
                    ShowPanel(shopPanel);
                    break;

                case GameState.RunEnd:
                    ShowPanel(runEndPanel);
                    break;
            }

            Debug.Log($"[GameManager] State: {oldState} → {newState}");

            EventBus.Instance.Publish(new GameStateChangedEvent
            {
                OldState = oldState,
                NewState = newState
            });
        }

        /// <summary>Called from Main Menu UI: starts a new run.</summary>
        public void StartNewRun()
        {
            TransitionTo(GameState.RunSetup);
        }

        /// <summary>Called from Map UI or encounter flow to proceed to the current node.</summary>
        public void ProceedToCurrentNode()
        {
            if (RunManager.Instance == null || !RunManager.Instance.IsRunActive) return;

            var nodeType = RunManager.Instance.CurrentNodeType;

            EventBus.Instance.Publish(new NodeSelectedEvent
            {
                NodeIndex = RunManager.Instance.CurrentNodeIndex,
                NodeType = nodeType
            });

            switch (nodeType)
            {
                case NodeType.Encounter:
                    TransitionTo(GameState.Encounter);
                    break;
                case NodeType.Shop:
                    TransitionTo(GameState.Shop);
                    break;
                case NodeType.Boss:
                    TransitionTo(GameState.Boss);
                    break;
                default:
                    TransitionTo(GameState.Encounter);
                    break;
            }
        }

        /// <summary>Called after encounter/shop completes to return to the map.</summary>
        public void ReturnToMap()
        {
            if (RunManager.Instance != null && RunManager.Instance.IsRunActive)
            {
                RunManager.Instance.AdvanceNode();
                // AdvanceNode may end the run if all nodes complete
                if (RunManager.Instance.IsRunActive)
                    TransitionTo(GameState.Map);
            }
        }

        // ── Event Handlers ────────────────────────────────────

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            // After encounter, return to map (with small delay for UX)
            // The ReturnToMap call advances the node
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            TransitionTo(GameState.RunEnd);
        }

        // ── Helpers ───────────────────────────────────────────

        private void HideAllPanels()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(mapPanel, false);
            SetPanelActive(encounterPanel, false);
            SetPanelActive(shopPanel, false);
            SetPanelActive(runEndPanel, false);
        }

        private void ShowPanel(GameObject panel)
        {
            SetPanelActive(panel, true);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
