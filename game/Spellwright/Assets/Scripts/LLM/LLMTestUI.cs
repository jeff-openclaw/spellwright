using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Spellwright.Data;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Spellwright.LLM
{
    /// <summary>
    /// Test UI for verifying LLM clue generation and streaming.
    /// Wire up in the LLMTestScene via the Inspector.
    /// </summary>
    public class LLMTestUI : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputField wordInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button streamButton;

        [Header("Output")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text outputText;

        // Thread-safe queue for streaming tokens from background thread → main thread
        private readonly ConcurrentQueue<string> _tokenQueue = new ConcurrentQueue<string>();
        private bool _isStreaming;
        private bool _isGenerating;

        private void Start()
        {
            sendButton.onClick.AddListener(OnSendClicked);
            streamButton.onClick.AddListener(OnStreamClicked);
            outputText.text = "";
            UpdateStatus();
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

            statusText.text = $"Model: {model} | Fallback: {fallback}{activity}";
        }

        // ── Clue Generation (Send) ─────────────────────────

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

            _isGenerating = true;
            outputText.text = $"Generating clue for \"{word}\"...\n";

            // Create a simple test NPC
            var testNPC = new NPCPromptData
            {
                DisplayName = "Test Riddlemaster",
                Archetype = NPCArchetype.Riddlemaster,
                SystemPromptTemplate = "You are {displayName}, a wise {archetype}. You speak in thoughtful riddles.",
                DifficultyModifier = 1.0f,
                IsBoss = false
            };

            var sw = Stopwatch.StartNew();
            var clue = await LLMManager.Instance.GenerateClueAsync(
                testNPC, word, "test", 1, new List<string>(), new List<string>());
            sw.Stop();

            outputText.text = $"Word: {word}\n"
                + $"Clue: {clue.Clue}\n"
                + $"Mood: {clue.Mood}\n"
                + $"Fallback: {clue.UsedFallbackModel}\n"
                + $"Time: {sw.ElapsedMilliseconds}ms (gen: {clue.GenerationTimeMs:F0}ms)";

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

            await LLMManager.Instance.StreamChatAsync(
                "You are a helpful assistant in a word-guessing game. Be concise.",
                prompt,
                token => _tokenQueue.Enqueue(token),
                () => _tokenQueue.Enqueue("\n\n[Done]"));

            _isStreaming = false;
        }
    }
}
