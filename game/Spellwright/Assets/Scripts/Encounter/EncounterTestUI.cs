using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
using Spellwright.Tomes;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Test UI for playing through word-guessing encounters.
    /// Wire up WordPools, NPC assets, and GameConfig in the Inspector.
    /// </summary>
    public class EncounterTestUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WordPoolSO[] wordPools;
        [SerializeField] private NPCDataSO[] npcAssets;
        [SerializeField] private GameConfigSO gameConfig;

        [Header("Controls")]
        [SerializeField] private Dropdown npcDropdown;
        [SerializeField] private Dropdown difficultyDropdown;
        [SerializeField] private InputField guessInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button newEncounterButton;

        [Header("Display")]
        [SerializeField] private Text blanksText;
        [SerializeField] private Text clueText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text logText;

        [Header("Tomes")]
        [SerializeField] private TomeDataSO[] testTomeAssets;
        [SerializeField] private Text tomeListText;
        [SerializeField] private Text tomeTriggerLogText;

        private EncounterManager _encounter;
        private bool _isProcessing;
        private bool _isBossEncounter;
        private readonly Dictionary<string, Text> _tomeToggleLabels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Image> _tomeToggleBgs = new Dictionary<string, Image>();

        // Boss UI
        private static readonly Color BossAccentColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        private Color _defaultStatusColor;

        private void Start()
        {
            // Get or add EncounterManager
            _encounter = GetComponent<EncounterManager>();
            if (_encounter == null)
                _encounter = gameObject.AddComponent<EncounterManager>();

            SetupDropdowns();

            submitButton.onClick.AddListener(OnSubmitClicked);
            newEncounterButton.onClick.AddListener(OnNewEncounterClicked);

            // Subscribe to events
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Instance.Subscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Subscribe<TomeEquippedEvent>(OnTomeEquipped);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);

            // Store default colors
            _defaultStatusColor = statusText != null ? statusText.color : Color.white;

            // Initial state
            blanksText.text = "";
            clueText.text = "Press 'New Encounter' to begin.";
            logText.text = "";
            if (tomeListText != null) tomeListText.text = "";
            if (tomeTriggerLogText != null) tomeTriggerLogText.text = "";
            submitButton.interactable = false;

            // Create per-tome toggle buttons
            CreateTomeToggles();

            UpdateStatus();
        }

        private void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<ClueReceivedEvent>(OnClueReceived);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Instance.Unsubscribe<TomeTriggeredEvent>(OnTomeTriggered);
            EventBus.Instance.Unsubscribe<TomeEquippedEvent>(OnTomeEquipped);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);
        }

        // ── Setup ────────────────────────────────────────────

        private void SetupDropdowns()
        {
            // NPC dropdown
            if (npcDropdown != null)
            {
                npcDropdown.ClearOptions();
                var npcOptions = new List<string>();
                if (npcAssets != null)
                {
                    foreach (var npc in npcAssets)
                    {
                        string label = npc != null ? npc.displayName : "(null)";
                        if (npc != null && npc.isBoss) label += " [BOSS]";
                        npcOptions.Add(label);
                    }
                }
                if (npcOptions.Count == 0) npcOptions.Add("(no NPCs assigned)");
                npcDropdown.AddOptions(npcOptions);
            }

            // Difficulty dropdown (1-5)
            if (difficultyDropdown != null)
            {
                difficultyDropdown.ClearOptions();
                difficultyDropdown.AddOptions(new List<string> { "1", "2", "3", "4", "5" });
            }
        }

        // ── Button Handlers ──────────────────────────────────

        private void OnNewEncounterClicked()
        {
            if (_isProcessing) return;
            if (wordPools == null || wordPools.Length == 0)
            {
                AppendLog("ERROR: No word pools assigned.");
                return;
            }
            if (npcAssets == null || npcAssets.Length == 0)
            {
                AppendLog("ERROR: No NPC assets assigned.");
                return;
            }

            // Ensure a run is active via RunManager
            if (RunManager.Instance != null && !RunManager.Instance.IsRunActive)
            {
                RunManager.Instance.StartRun();
                AppendLog("New run started.");
            }

            // Select a random pool
            var pool = wordPools[Random.Range(0, wordPools.Length)];
            int npcIndex = npcDropdown != null ? npcDropdown.value : 0;
            var npc = npcAssets[Mathf.Clamp(npcIndex, 0, npcAssets.Length - 1)];
            int difficulty = (difficultyDropdown != null ? difficultyDropdown.value : 0) + 1;

            // Get used words from RunManager
            var usedWords = RunManager.Instance != null
                ? new List<string>(RunManager.Instance.UsedWords)
                : new List<string>();

            logText.text = "";
            clueText.text = "Generating first clue...";

            if (npc.isBoss)
            {
                AppendLog($"Starting BOSS encounter — Pool: {pool.category}, NPC: {npc.displayName}, Difficulty: 3-4");
                _encounter.StartEncounter(pool, npc, usedWords, 3, 4);
            }
            else
            {
                AppendLog($"Starting encounter — Pool: {pool.category}, NPC: {npc.displayName}, Difficulty: {difficulty}");
                _encounter.StartEncounter(pool, npc, usedWords, difficulty);
            }
        }

        private async void OnSubmitClicked()
        {
            if (_isProcessing || !_encounter.IsActive) return;

            var guess = guessInput.text.Trim();
            if (string.IsNullOrEmpty(guess)) return;

            _isProcessing = true;
            submitButton.interactable = false;
            guessInput.text = "";

            await _encounter.SubmitGuess(guess);

            _isProcessing = false;
            if (_encounter.IsActive)
                submitButton.interactable = true;
        }

        // ── Event Handlers ───────────────────────────────────

        private string _currentBlanks;

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            // Show blanks
            _currentBlanks = "";
            for (int i = 0; i < evt.TargetWord.Length; i++)
            {
                if (i > 0) _currentBlanks += " ";
                _currentBlanks += "_";
            }
            blanksText.text = _currentBlanks;
            submitButton.interactable = true;
            AppendLog($"NPC: {evt.NPC.DisplayName} | Category: {evt.Category} | Letters: {evt.TargetWord.Length}");
            UpdateStatus();
        }

        private void OnClueReceived(ClueReceivedEvent evt)
        {
            string fallbackNote = evt.Clue.UsedFallbackModel ? " [fallback]" : "";
            clueText.text = $"Clue #{evt.ClueNumber}: {evt.Clue.Clue}";
            AppendLog($"Clue #{evt.ClueNumber}{fallbackNote}: {evt.Clue.Clue}");
            UpdateStatus();
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            string icon = evt.Result.IsCorrect ? "OK" : evt.Result.IsValidWord ? "X" : "?";
            AppendLog($"[{icon}] \"{evt.Guess}\" — {evt.Result.Feedback}");
            if (evt.Result.IsValidWord && !evt.Result.IsCorrect)
                AppendLog($"    Letters in correct position: {evt.Result.LettersCorrect}");
            UpdateStatus();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            submitButton.interactable = false;

            string bossTag = evt.IsBoss ? "BOSS " : "";
            string result = evt.Won
                ? $"{bossTag}YOU WON! Score: {evt.Score}"
                : $"{bossTag}YOU LOST!";
            blanksText.text = evt.TargetWord.ToUpperInvariant();
            AppendLog($"─── {result} ───");
            AppendLog($"The word was: \"{evt.TargetWord}\" | Guesses: {evt.GuessCount}");

            // Reset boss styling
            if (_isBossEncounter)
            {
                _isBossEncounter = false;
                ResetBossUI();
            }

            UpdateStatus();
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            _isBossEncounter = true;
            ApplyBossUI();

            AppendLog($"═══ BOSS: {evt.BossName} ═══");
            AppendLog(evt.IntroText);
            clueText.text = evt.IntroText;
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            AppendLog($"HP: {evt.OldHP} → {evt.NewHP} / {evt.MaxHP}");
            UpdateStatus();
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            submitButton.interactable = false;
            string outcome = evt.Won ? "VICTORY" : "DEFEAT";
            AppendLog($"═══ RUN OVER: {outcome} — Final Score: {evt.FinalScore} ═══");
            UpdateStatus();
        }

        // ── Tome Event Handlers ─────────────────────────────

        private void OnTomeTriggered(TomeTriggeredEvent evt)
        {
            AppendLog($"TOME [{evt.TomeName}]: {evt.RevealedInfo}");
            AppendTomeLog($"[{evt.TomeName}] {evt.RevealedInfo}");

            // Update blanks display for letter-revealing tomes
            if (_currentBlanks != null && evt.TomeName == "First Light"
                && evt.RevealedInfo != null && evt.RevealedInfo.Length > 0)
            {
                // Replace first "_" with the revealed letter
                char revealed = evt.RevealedInfo[evt.RevealedInfo.Length - 1];
                if (char.IsLetter(revealed))
                {
                    _currentBlanks = revealed.ToString().ToUpperInvariant() + _currentBlanks.Substring(1);
                    blanksText.text = _currentBlanks;
                }
            }
        }

        private void OnTomeEquipped(TomeEquippedEvent evt)
        {
            UpdateTomeList();
        }

        private void CreateTomeToggles()
        {
            if (tomeListText == null || TomeManager.Instance == null || testTomeAssets == null)
            {
                if (tomeListText != null) tomeListText.text = "(no TomeManager)";
                return;
            }

            tomeListText.text = "Tomes (click to toggle):";

            // Create a container for toggle buttons on the right half of the bottom area
            var containerGO = new GameObject("TomeTogglesContainer");
            containerGO.transform.SetParent(tomeListText.transform.parent, false);

            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.0f);
            containerRT.anchorMax = new Vector2(0.95f, 0.14f);
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;

            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 2;

            foreach (var tome in testTomeAssets)
            {
                if (tome == null) continue;
                CreateTomeToggleButton(tome, containerGO.transform);
            }
        }

        private void CreateTomeToggleButton(TomeDataSO tome, Transform parent)
        {
            var btnGO = new GameObject($"TomeBtn_{tome.tomeId}");
            btnGO.transform.SetParent(parent, false);

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(5, 0);
            labelRT.offsetMax = new Vector2(-5, 0);

            var label = labelGO.AddComponent<Text>();
            label.text = $"[ ] {tome.displayName}";
            label.font = tomeListText.font;
            label.fontSize = Mathf.Max(tomeListText.fontSize - 2, 10);
            label.color = tomeListText.color;
            label.alignment = TextAnchor.MiddleLeft;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            _tomeToggleLabels[tome.tomeId] = label;
            _tomeToggleBgs[tome.tomeId] = img;

            var capturedTome = tome;
            btn.onClick.AddListener(() => ToggleTome(capturedTome));
        }

        private static readonly Color TomeOffColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        private static readonly Color TomeOnColor = new Color(0.15f, 0.5f, 0.15f, 0.8f);

        private void ToggleTome(TomeDataSO tome)
        {
            // Check if currently equipped by trying to unequip first
            bool wasEquipped = TomeManager.Instance.UnequipTome(tome.tomeId);
            if (!wasEquipped)
                TomeManager.Instance.EquipTome(tome);

            bool isNowEquipped = !wasEquipped;
            if (_tomeToggleLabels.TryGetValue(tome.tomeId, out var label))
            {
                string marker = isNowEquipped ? "[x]" : "[ ]";
                label.text = $"{marker} {tome.displayName}";
            }
            if (_tomeToggleBgs.TryGetValue(tome.tomeId, out var bg))
                bg.color = isNowEquipped ? TomeOnColor : TomeOffColor;
        }

        private void UpdateTomeList()
        {
            // Toggle labels are updated directly in ToggleTome, nothing extra needed
        }

        private void AppendTomeLog(string message)
        {
            if (tomeTriggerLogText == null) return;
            if (tomeTriggerLogText.text.Length > 0)
                tomeTriggerLogText.text += "\n";
            tomeTriggerLogText.text += message;
        }

        // ── Boss UI ──────────────────────────────────────────

        private void ApplyBossUI()
        {
            if (statusText != null)
                statusText.color = BossAccentColor;
            if (blanksText != null)
                blanksText.color = BossAccentColor;
            if (clueText != null)
                clueText.color = BossAccentColor;
        }

        private void ResetBossUI()
        {
            if (statusText != null)
                statusText.color = _defaultStatusColor;
            if (blanksText != null)
                blanksText.color = _defaultStatusColor;
            if (clueText != null)
                clueText.color = _defaultStatusColor;
        }

        // ── Helpers ──────────────────────────────────────────

        private void UpdateStatus()
        {
            if (_encounter == null || gameConfig == null)
            {
                statusText.text = "Status: Not initialized";
                return;
            }

            // Show run-level info if RunManager is available
            string runInfo = "";
            if (RunManager.Instance != null && RunManager.Instance.IsRunActive)
            {
                runInfo = $"Run Score: {RunManager.Instance.Score} | ";
            }

            if (!_encounter.IsActive)
            {
                bool runOver = RunManager.Instance != null && !RunManager.Instance.IsRunActive;
                string idleMsg = runOver
                    ? "Run over — press New Encounter to start a new run"
                    : "Idle — press New Encounter";
                statusText.text = $"{runInfo}HP: {_encounter.CurrentHP}/{_encounter.MaxHP} | {idleMsg}";
                return;
            }

            statusText.text = $"{runInfo}HP: {_encounter.CurrentHP}/{_encounter.MaxHP} | "
                + $"Clue #{_encounter.CurrentClueNumber} | "
                + $"Guesses left: {_encounter.GuessesRemaining}";
        }

        private void AppendLog(string message)
        {
            if (logText == null) return;
            if (logText.text.Length > 0)
                logText.text += "\n";
            logText.text += message;
        }
    }
}
