using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Encounter;
using Spellwright.ScriptableObjects;
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

        [Header("Encounter Setup")]
        [SerializeField] private NPCDataSO bossNPC;
        [SerializeField] private WordPoolSO[] wordPools;
        [SerializeField] private WordPoolSO[] wordPoolsRo;
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("NPCs (ordered by difficulty: easy → hard)")]
        [SerializeField] private NPCDataSO[] regularNPCs;

        private int _encounterCount;

        /// <summary>Returns the word pool array for the configured language.</summary>
        private WordPoolSO[] ActiveWordPools =>
            gameConfig != null && gameConfig.language == Data.GameLanguage.Romanian && wordPoolsRo != null && wordPoolsRo.Length > 0
                ? wordPoolsRo
                : wordPools;

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
                    _encounterCount = 0;
                    if (RunManager.Instance != null)
                    {
                        RunManager.Instance.StartRun();
                        RunManager.Instance.GenerateIntel(ActiveWordPools);
                    }
                    TransitionTo(GameState.Map);
                    return;

                case GameState.Map:
                    ShowPanel(mapPanel);
                    break;

                case GameState.Encounter:
                    ShowPanel(encounterPanel);
                    StartRegularEncounter();
                    break;

                case GameState.Boss:
                    ShowPanel(encounterPanel);
                    StartBossEncounter();
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

        /// <summary>Called from Map UI to proceed to the current node.</summary>
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
                case NodeType.Boss:
                    TransitionTo(GameState.Boss);
                    break;
                default:
                    TransitionTo(GameState.Encounter);
                    break;
            }
        }

        /// <summary>Called after encounter result to show the shop before returning to map.</summary>
        public void GoToShop()
        {
            TransitionTo(GameState.Shop);
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
            if (evt.IsBoss)
            {
                if (evt.Won)
                {
                    // Boss victory — start next wave, show shop, then back to map
                    if (RunManager.Instance != null && RunManager.Instance.IsRunActive)
                    {
                        RunManager.Instance.StartNextWave();
                        RunManager.Instance.GenerateIntel(ActiveWordPools);
                    }
                    GoToShop();
                }
                else
                {
                    // Boss loss = run loss (even if HP remains)
                    if (RunManager.Instance != null && RunManager.Instance.IsRunActive)
                        RunManager.Instance.EndRun(won: false);
                }
            }
            // Non-boss encounters: EncounterUI shows result + continue button → calls ReturnToMap
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            TransitionTo(GameState.RunEnd);
        }

        // ── Encounters ───────────────────────────────────────

        /// <summary>Starts a regular encounter with progressive difficulty and NPC selection.</summary>
        private void StartRegularEncounter()
        {
            var pools = ActiveWordPools;
            if (encounterManager == null || pools == null || pools.Length == 0)
            {
                Debug.LogWarning("[GameManager] Regular encounter missing references.");
                return;
            }

            _encounterCount++;

            // First encounter: tutorial mode with phrase + first/last letter reveal
            if (_encounterCount == 1)
            {
                StartFirstEncounter(pools);
                return;
            }

            var usedWords = RunManager.Instance != null
                ? new List<string>(RunManager.Instance.UsedWords)
                : new List<string>();

            // Select NPC based on encounter progression
            NPCDataSO npc = SelectNPCForEncounter(_encounterCount);

            // Get difficulty from config
            Vector2Int diff = gameConfig != null
                ? gameConfig.GetDifficultyForEncounter(_encounterCount)
                : new Vector2Int(1, 2);

            // Mid/late encounters: chance to select a phrase instead of a single word
            float phraseChance = _encounterCount >= 5 ? 0.5f : (_encounterCount >= 3 ? 0.4f : 0f);
            if (phraseChance > 0f && Random.value < phraseChance)
            {
                var phraseCandidates = new List<Data.WordEntry>();
                foreach (var p in pools)
                {
                    var words = p.GetWordsByDifficultyRange(diff.x, diff.y);
                    phraseCandidates.AddRange(words.FindAll(w => w.IsPhrase && !usedWords.Contains(w.Word)));
                }
                if (phraseCandidates.Count > 0)
                {
                    Debug.Log($"[GameManager] Encounter #{_encounterCount}: phrase selected ({phraseCandidates.Count} candidates, chance={phraseChance:P0})");
                    encounterManager.StartEncounter(phraseCandidates, npc, usedWords);
                    return;
                }
            }

            // Regular single-word encounter
            var pool = pools[Random.Range(0, pools.Length)];
            int difficulty = Random.Range(diff.x, diff.y + 1);

            Debug.Log($"[GameManager] Starting encounter #{_encounterCount}: NPC={npc?.displayName}, Difficulty={difficulty}");
            encounterManager.StartEncounter(pool, npc, usedWords, difficulty);
        }

        /// <summary>First encounter: picks a phrase (2+ words) and uses tutorial mode with first/last letter reveal.</summary>
        private void StartFirstEncounter(WordPoolSO[] pools)
        {
            var usedWords = RunManager.Instance != null
                ? new List<string>(RunManager.Instance.UsedWords)
                : new List<string>();

            NPCDataSO npc = SelectNPCForEncounter(1);

            Vector2Int diff = gameConfig != null
                ? gameConfig.GetDifficultyForEncounter(1)
                : new Vector2Int(1, 2);

            // Collect phrase candidates (2+ words) from all pools at easy difficulty
            var phraseCandidates = new List<Data.WordEntry>();
            foreach (var pool in pools)
            {
                var words = pool.GetWordsByDifficultyRange(diff.x, diff.y);
                phraseCandidates.AddRange(words.FindAll(w => w.IsPhrase));
            }

            if (phraseCandidates.Count > 0)
            {
                Debug.Log($"[GameManager] First encounter (tutorial): {phraseCandidates.Count} phrase candidates found");
                encounterManager.StartEncounter(phraseCandidates, npc, usedWords, isFirstEncounter: true);
            }
            else
            {
                // Fallback: normal encounter if no phrases available
                Debug.LogWarning("[GameManager] No phrases found for first encounter, falling back to normal word.");
                var pool = pools[Random.Range(0, pools.Length)];
                int difficulty = Random.Range(diff.x, diff.y + 1);
                encounterManager.StartEncounter(pool, npc, usedWords, difficulty);
            }
        }

        /// <summary>
        /// Picks an NPC based on encounter number for progressive difficulty.
        /// Array order: [0]=Guide (tutorial), [1]=Riddlemaster, [2]=Merchant, [3]=Librarian
        /// </summary>
        private NPCDataSO SelectNPCForEncounter(int encounterNumber)
        {
            if (regularNPCs == null || regularNPCs.Length == 0)
            {
                Debug.LogWarning("[GameManager] No regular NPCs assigned, falling back to first available.");
                return null;
            }

            // Encounter 1: tutorial guide (index 0)
            // Early encounters (2-3): easy NPC (index 1)
            // Mid encounters (4-5): mid NPC (index 2)
            // Late encounters (6+): hard NPC (index 3)
            int npcIndex;
            if (encounterNumber <= 1)
                npcIndex = 0;
            else if (encounterNumber <= 3)
                npcIndex = Mathf.Min(1, regularNPCs.Length - 1);
            else if (encounterNumber <= 5)
                npcIndex = Mathf.Min(2, regularNPCs.Length - 1);
            else
                npcIndex = Mathf.Min(3, regularNPCs.Length - 1);

            return regularNPCs[npcIndex];
        }

        /// <summary>Returns the NPC that would be assigned to a given node index. Used for map dossier preview.</summary>
        public NPCDataSO PreviewNPCForNode(int nodeIndex, NodeType nodeType)
        {
            if (nodeType == NodeType.Boss)
                return bossNPC;

            // Simulate encounter number based on node index (non-boss nodes before this one)
            int encounterNumber = nodeIndex + 1;
            return SelectNPCForEncounter(encounterNumber);
        }

        /// <summary>Returns the word category for a given node index. Used for map dossier preview.</summary>
        public string PreviewCategoryForNode(int nodeIndex)
        {
            var pools = ActiveWordPools;
            if (pools == null || pools.Length == 0) return "???";
            int poolIndex = nodeIndex % pools.Length;
            return pools[poolIndex] != null ? pools[poolIndex].name : "???";
        }

        private void StartBossEncounter()
        {
            var bossPools = ActiveWordPools;
            if (encounterManager == null || bossNPC == null || bossPools == null || bossPools.Length == 0)
            {
                Debug.LogWarning("[GameManager] Boss encounter missing references (encounterManager, bossNPC, or wordPools).");
                return;
            }

            var pool = bossPools[Random.Range(0, bossPools.Length)];
            var usedWords = RunManager.Instance != null
                ? new List<string>(RunManager.Instance.UsedWords)
                : new List<string>();

            var bossDiff = gameConfig != null ? gameConfig.bossDifficulty : new Vector2Int(3, 4);
            encounterManager.StartEncounter(pool, bossNPC, usedWords, bossDiff.x, bossDiff.y);
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
