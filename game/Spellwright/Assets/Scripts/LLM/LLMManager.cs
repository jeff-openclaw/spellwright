using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Spellwright.Data;
using Spellwright.ScriptableObjects;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Spellwright.LLM
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the LLM lifecycle and provides
    /// the primary API for clue generation with automatic fallback.
    ///
    /// <para>
    /// Wraps <see cref="LLMService"/> for model loading/inference and
    /// <see cref="FallbackClueService"/> for static clues when the LLM
    /// is unavailable or returns invalid responses.
    /// </para>
    /// </summary>
    public class LLMManager : MonoBehaviour
    {
        public static LLMManager Instance { get; private set; }

        [SerializeField] private GameConfigSO gameConfig;
        [SerializeField] private TextAsset fallbackCluesAsset;

        private LLMService _llmService;
        private FallbackClueService _fallbackService;
        private CancellationTokenSource _cts;

        /// <summary>Whether the LLM model is loaded and ready for inference.</summary>
        public bool IsModelLoaded => _llmService?.IsLoaded ?? false;

        /// <summary>Whether fallback clues are available.</summary>
        public bool IsFallbackAvailable => _fallbackService?.IsLoaded ?? false;

        /// <summary>Whether at least one clue source (LLM or fallback) is ready.</summary>
        public bool IsReady => IsModelLoaded || IsFallbackAvailable;

        // ── Lifecycle ───────────────────────────────────────

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

        private async void Start()
        {
            _cts = new CancellationTokenSource();

            // Load fallback clues synchronously (small JSON, instant)
            LoadFallbackClues();

            // Load LLM model asynchronously
            await LoadLLMModelAsync();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _llmService?.Dispose();
            _llmService = null;
        }

        // ── Initialization ──────────────────────────────────

        private void LoadFallbackClues()
        {
            _fallbackService = new FallbackClueService();

            if (fallbackCluesAsset == null)
            {
                Debug.LogWarning("[LLMManager] No fallback clues TextAsset assigned.");
                return;
            }

            _fallbackService.LoadFromJson(fallbackCluesAsset.text);
            if (_fallbackService.IsLoaded)
                Debug.Log($"[LLMManager] Fallback clues ready ({_fallbackService.WordCount} words).");
        }

        private async Task LoadLLMModelAsync()
        {
            _llmService = new LLMService();

            if (gameConfig != null)
            {
                _llmService.ModelFilename = gameConfig.modelFileName;
                _llmService.MaxTokens = gameConfig.maxTokens;
                _llmService.Temperature = gameConfig.temperature;
            }

            var success = await _llmService.LoadModelAsync(_cts.Token);
            if (success)
                Debug.Log("[LLMManager] LLM model ready.");
            else
                Debug.LogWarning("[LLMManager] LLM model failed to load. Using fallback clues only.");
        }

        // ── Public API: Clue Generation ─────────────────────

        /// <summary>
        /// Generates a clue for the given word. Tries the LLM first, then falls
        /// back to static clues, then to a generic last-resort clue.
        /// </summary>
        /// <param name="npc">NPC personality data for prompt building.</param>
        /// <param name="word">The secret target word.</param>
        /// <param name="category">Word category (e.g. "animals").</param>
        /// <param name="clueNumber">1-based clue index.</param>
        /// <param name="previousGuesses">Player's previous guesses (may be null).</param>
        /// <param name="activeTomeEffects">Active Tome effect names (may be null).</param>
        /// <returns>A <see cref="ClueResponse"/> — never null.</returns>
        public async Task<ClueResponse> GenerateClueAsync(
            NPCPromptData npc,
            string word,
            string category,
            int clueNumber,
            List<string> previousGuesses = null,
            List<string> activeTomeEffects = null)
        {
            // Try LLM first
            if (IsModelLoaded)
            {
                var llmClue = await TryLLMClueAsync(npc, word, category, clueNumber,
                    previousGuesses, activeTomeEffects);
                if (llmClue != null)
                    return llmClue;

                Debug.LogWarning("[LLMManager] LLM clue generation failed, trying fallback.");
            }

            // Fall back to static clues
            if (IsFallbackAvailable && _fallbackService.HasWord(word))
            {
                Debug.Log($"[LLMManager] Using fallback clue for \"{word}\" (clue #{clueNumber}).");
                return _fallbackService.GetClue(word, clueNumber);
            }

            // Last resort: generic clue
            Debug.LogWarning($"[LLMManager] No clue source available for \"{word}\". Using generic clue.");
            return new ClueResponse
            {
                Clue = $"Think about the category \"{category}\" — the answer has {word.Length} letters.",
                Mood = "neutral",
                UsedFallbackModel = true,
                GenerationTimeMs = 0f
            };
        }

        private async Task<ClueResponse> TryLLMClueAsync(
            NPCPromptData npc,
            string word,
            string category,
            int clueNumber,
            List<string> previousGuesses,
            List<string> activeTomeEffects)
        {
            try
            {
                var (systemPrompt, userMessage) = PromptBuilder.BuildCluePrompt(
                    npc, word, category, clueNumber,
                    previousGuesses ?? new List<string>(),
                    activeTomeEffects ?? new List<string>());

                var sw = Stopwatch.StartNew();
                var raw = await _llmService.ChatAsync(systemPrompt, userMessage, _cts.Token);
                sw.Stop();

                if (raw == null)
                    return null;

                var parsed = ResponseParser.ParseClueResponse(raw, word);
                if (parsed == null)
                    return null;

                parsed.UsedFallbackModel = false;
                parsed.GenerationTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                return parsed;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMManager] LLM clue error: {ex.Message}");
                return null;
            }
        }

        // ── Public API: Raw Streaming ───────────────────────

        /// <summary>
        /// Streams a raw chat completion token-by-token. LLM only — no fallback.
        /// Callbacks may fire on a background thread.
        /// </summary>
        public async Task StreamChatAsync(
            string systemPrompt,
            string userMessage,
            Action<string> onToken,
            Action onDone)
        {
            if (!IsModelLoaded)
            {
                Debug.LogWarning("[LLMManager] Cannot stream — model not loaded.");
                onDone?.Invoke();
                return;
            }

            await _llmService.StreamChatAsync(systemPrompt, userMessage, onToken, onDone, _cts.Token);
        }

        // ── Public API: JSON Mode ───────────────────────────

        /// <summary>
        /// Sends a chat request expecting a JSON response. LLM only — no fallback.
        /// </summary>
        public async Task<T> ChatJsonAsync<T>(string systemPrompt, string userMessage)
        {
            if (!IsModelLoaded)
            {
                Debug.LogWarning("[LLMManager] Cannot use JSON mode — model not loaded.");
                return default;
            }

            return await _llmService.ChatJsonAsync<T>(systemPrompt, userMessage, _cts.Token);
        }
    }
}
