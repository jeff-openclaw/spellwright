using System.Collections.Generic;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.ScriptableObjects;
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

        private EncounterManager _encounter;
        private bool _isProcessing;

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

            // Initial state
            blanksText.text = "";
            clueText.text = "Press 'New Encounter' to begin.";
            logText.text = "";
            submitButton.interactable = false;
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
            AppendLog($"Starting encounter — Pool: {pool.category}, NPC: {npc.displayName}, Difficulty: {difficulty}");

            _encounter.StartEncounter(pool, npc, usedWords, difficulty);
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

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            // Show blanks
            string blanks = "";
            for (int i = 0; i < evt.TargetWord.Length; i++)
            {
                if (i > 0) blanks += " ";
                blanks += "_";
            }
            blanksText.text = blanks;
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
            string result = evt.Won
                ? $"YOU WON! Score: {evt.Score}"
                : $"YOU LOST!";
            blanksText.text = evt.TargetWord.ToUpperInvariant();
            AppendLog($"─── {result} ───");
            AppendLog($"The word was: \"{evt.TargetWord}\" | Guesses: {evt.GuessCount}");
            UpdateStatus();
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
