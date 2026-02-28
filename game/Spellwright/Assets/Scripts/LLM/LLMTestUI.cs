using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Spellwright.LLM
{
    /// <summary>
    /// Test UI for verifying LLM clue generation with all NPC archetypes.
    /// Supports NPC selection, sequential clue generation, and full pipeline testing.
    /// Wire up in the LLMTestScene via the Inspector.
    /// </summary>
    public class LLMTestUI : MonoBehaviour
    {
        [Header("NPC Selection")]
        [SerializeField] private NPCDataSO[] npcAssets;
        [SerializeField] private Dropdown npcDropdown;

        [Header("Input")]
        [SerializeField] private InputField wordInput;
        [SerializeField] private InputField categoryInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button nextClueButton;
        [SerializeField] private Button streamButton;

        [Header("Output")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text outputText;

        // Thread-safe queue for streaming tokens from background thread → main thread
        private readonly ConcurrentQueue<string> _tokenQueue = new ConcurrentQueue<string>();
        private bool _isStreaming;
        private bool _isGenerating;

        // Sequential clue state
        private string _currentWord;
        private string _currentCategory;
        private int _clueNumber;
        private readonly List<string> _previousGuesses = new List<string>();
        private NPCPromptData _currentNPC;

        private void Start()
        {
            sendButton.onClick.AddListener(OnSendClicked);
            nextClueButton.onClick.AddListener(OnNextClueClicked);
            streamButton.onClick.AddListener(OnStreamClicked);

            SetupNPCDropdown();

            outputText.text = "";
            if (nextClueButton != null)
                nextClueButton.interactable = false;
            UpdateStatus();
        }

        private void SetupNPCDropdown()
        {
            if (npcDropdown == null) return;

            npcDropdown.ClearOptions();
            var options = new List<string>();

            if (npcAssets != null && npcAssets.Length > 0)
            {
                foreach (var npc in npcAssets)
                {
                    string label = npc != null ? npc.displayName : "(null)";
                    if (npc != null && npc.isBoss) label += " [BOSS]";
                    options.Add(label);
                }
            }
            else
            {
                // Fallback built-in options when no assets are wired
                options.Add("Riddlemaster");
                options.Add("Trickster Merchant");
                options.Add("Silent Librarian");
                options.Add("The Whisperer [BOSS]");
            }

            npcDropdown.AddOptions(options);
        }

        private void Update()
        {
            // Drain token queue on main thread
            while (_tokenQueue.TryDequeue(out var token))
            {
                outputText.text += token;
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (LLMManager.Instance == null)
            {
                statusText.text = "Status: LLMManager not found";
                return;
            }

            var mgr = LLMManager.Instance;
            string model = mgr.IsModelLoaded ? "Loaded" : "Not loaded";
            string fallback = mgr.IsFallbackAvailable ? "Available" : "Not available";
            string activity = _isGenerating ? " | Generating..." : _isStreaming ? " | Streaming..." : "";
            string clueInfo = _clueNumber > 0 ? $" | Clue #{_clueNumber}" : "";

            statusText.text = $"Model: {model} | Fallback: {fallback}{activity}{clueInfo}";
        }

        // ── NPC Selection ────────────────────────────────────

        private NPCPromptData GetSelectedNPC()
        {
            int index = npcDropdown != null ? npcDropdown.value : 0;

            // Use wired ScriptableObject assets if available
            if (npcAssets != null && index < npcAssets.Length && npcAssets[index] != null)
            {
                return npcAssets[index].ToPromptData();
            }

            // Fallback: create built-in NPCs
            return index switch
            {
                1 => new NPCPromptData
                {
                    DisplayName = "Trickster Merchant",
                    Archetype = NPCArchetype.TricksterMerchant,
                    SystemPromptTemplate = "You are {displayName}, a sly {archetype}. You give sales pitches instead of clues.",
                    DifficultyModifier = 0.9f,
                    IsBoss = false
                },
                2 => new NPCPromptData
                {
                    DisplayName = "Silent Librarian",
                    Archetype = NPCArchetype.SilentLibrarian,
                    SystemPromptTemplate = "You are {displayName}, a terse {archetype}. You define words clinically.",
                    DifficultyModifier = 1.1f,
                    IsBoss = false
                },
                3 => new NPCPromptData
                {
                    DisplayName = "The Whisperer",
                    Archetype = NPCArchetype.Riddlemaster,
                    SystemPromptTemplate = "You are {displayName}. You speak in fragments. Exactly three words at a time.",
                    DifficultyModifier = 1.5f,
                    IsBoss = true,
                    BossConstraint = "Your clues must be exactly 3 words."
                },
                _ => new NPCPromptData
                {
                    DisplayName = "Riddlemaster",
                    Archetype = NPCArchetype.Riddlemaster,
                    SystemPromptTemplate = "You are {displayName}, a wise {archetype}. You speak in thoughtful riddles.",
                    DifficultyModifier = 1.0f,
                    IsBoss = false
                }
            };
        }

        // ── Clue Generation (Send — starts new sequence) ────

        private async void OnSendClicked()
        {
            if (_isGenerating || _isStreaming) return;

            var word = wordInput.text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(word))
            {
                outputText.text = "Enter a word first.";
                return;
            }

            if (LLMManager.Instance == null || !LLMManager.Instance.IsReady)
            {
                outputText.text = "LLMManager is not ready.";
                return;
            }

            // Reset sequence state
            _currentWord = word;
            _currentCategory = !string.IsNullOrEmpty(categoryInput?.text)
                ? categoryInput.text.Trim()
                : "general";
            _clueNumber = 1;
            _previousGuesses.Clear();
            _currentNPC = GetSelectedNPC();

            await GenerateClue();
        }

        // ── Next Clue (continues sequence) ───────────────────

        private async void OnNextClueClicked()
        {
            if (_isGenerating || _isStreaming) return;
            if (string.IsNullOrEmpty(_currentWord)) return;

            _clueNumber++;

            // Simulate a wrong guess for testing progressive hints
            string fakeGuess = $"guess{_clueNumber - 1}";
            _previousGuesses.Add(fakeGuess);

            await GenerateClue();
        }

        private async System.Threading.Tasks.Task GenerateClue()
        {
            _isGenerating = true;
            string npcLabel = _currentNPC.IsBoss
                ? $"{_currentNPC.DisplayName} [BOSS]"
                : _currentNPC.DisplayName;

            if (_clueNumber == 1)
            {
                outputText.text = $"NPC: {npcLabel}\n"
                    + $"Word: \"{_currentWord}\" ({_currentCategory})\n"
                    + $"─────────────────────────\n";
            }

            outputText.text += $"\n[Clue #{_clueNumber}] Generating...\n";

            var sw = Stopwatch.StartNew();
            var clue = await LLMManager.Instance.GenerateClueAsync(
                _currentNPC, _currentWord, _currentCategory, _clueNumber,
                _previousGuesses, new List<string>());
            sw.Stop();

            outputText.text += $"  Clue: {clue.Clue}\n"
                + $"  Mood: {clue.Mood}\n"
                + $"  Fallback: {clue.UsedFallbackModel}\n"
                + $"  Time: {sw.ElapsedMilliseconds}ms (gen: {clue.GenerationTimeMs:F0}ms)\n";

            if (_previousGuesses.Count > 0)
            {
                outputText.text += $"  Guesses so far: {string.Join(", ", _previousGuesses)}\n";
            }

            // Enable next clue button
            if (nextClueButton != null)
                nextClueButton.interactable = true;

            _isGenerating = false;
        }

        // ── Raw Streaming (Stream) ──────────────────────────

        private async void OnStreamClicked()
        {
            if (_isGenerating || _isStreaming) return;

            var prompt = wordInput.text.Trim();
            if (string.IsNullOrEmpty(prompt))
            {
                outputText.text = "Enter a prompt first.";
                return;
            }

            if (LLMManager.Instance == null || !LLMManager.Instance.IsModelLoaded)
            {
                outputText.text = "LLM model is not loaded. Streaming requires the model.";
                return;
            }

            _isStreaming = true;
            outputText.text = "";

            var npc = GetSelectedNPC();
            string systemPrompt = $"You are {npc.DisplayName}. {npc.SystemPromptTemplate}";

            await LLMManager.Instance.StreamChatAsync(
                systemPrompt,
                prompt,
                token => _tokenQueue.Enqueue(token),
                () => _tokenQueue.Enqueue("\n\n[Done]"));

            _isStreaming = false;
        }
    }
}
