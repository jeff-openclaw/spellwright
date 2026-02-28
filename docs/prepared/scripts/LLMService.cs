using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using Newtonsoft.Json;
using UnityEngine;

namespace Spellwright.LLM
{
    /// <summary>
    /// In-process LLM inference service using LLamaSharp (llama.cpp C# bindings).
    /// Replaces the previous OllamaService — no external server required.
    /// 
    /// <para>
    /// The model GGUF file ships with the game in StreamingAssets/Models/.
    /// Supports Metal (macOS), CUDA (Windows/Linux), Vulkan, and CPU fallback.
    /// </para>
    /// 
    /// <para>
    /// Thread-safe: uses a semaphore to serialize inference calls (LLamaContext
    /// is not thread-safe for concurrent generation).
    /// </para>
    /// </summary>
    public class LLMService : IDisposable
    {
        private LLamaWeights _model;
        private LLamaContext _context;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private bool _isLoaded;

        /// <summary>Path to the GGUF model file. Defaults to StreamingAssets/Models/.</summary>
        public string ModelPath { get; set; }

        /// <summary>Model filename within the models directory.</summary>
        public string ModelFilename { get; set; } = "llama-3.2-3b-q4_k_m.gguf";

        /// <summary>Context window size in tokens.</summary>
        public uint ContextSize { get; set; } = 2048;

        /// <summary>Number of GPU layers to offload. Set to 99 to offload all.</summary>
        public int GpuLayerCount { get; set; } = 99;

        /// <summary>Default generation temperature.</summary>
        public float Temperature { get; set; } = 0.8f;

        /// <summary>Default max tokens to generate per request.</summary>
        public int MaxTokens { get; set; } = 200;

        /// <summary>Timeout per inference request in seconds.</summary>
        public float TimeoutSeconds { get; set; } = 30f;

        /// <summary>Whether the model is loaded and ready for inference.</summary>
        public bool IsLoaded => _isLoaded;

        // ── Model Loading ──────────────────────────────────

        /// <summary>
        /// Loads the GGUF model from disk. Call this during a loading screen —
        /// it takes 2-4 seconds depending on hardware. Must be called from
        /// a background thread or via Task.Run() to avoid blocking Unity's main thread.
        /// </summary>
        /// <returns>True if model loaded successfully, false otherwise.</returns>
        public async Task<bool> LoadModelAsync(CancellationToken ct = default)
        {
            if (_isLoaded) return true;

            var path = GetModelPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[LLMService] Model file not found: {path}");
                return false;
            }

            try
            {
                var parameters = new ModelParams(path)
                {
                    ContextSize = ContextSize,
                    GpuLayerCount = GpuLayerCount,
                };

                // Load on thread pool to avoid blocking Unity main thread
                _model = await Task.Run(() => LLamaWeights.LoadFromFile(parameters), ct);
                _context = _model.CreateContext(parameters);
                _isLoaded = true;

                Debug.Log($"[LLMService] Model loaded: {ModelFilename} " +
                          $"(ctx={ContextSize}, gpu_layers={GpuLayerCount})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMService] Failed to load model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unloads the model and frees memory. Call when leaving the game scene
        /// or during cleanup.
        /// </summary>
        public void UnloadModel()
        {
            _context?.Dispose();
            _context = null;
            _model?.Dispose();
            _model = null;
            _isLoaded = false;
            Debug.Log("[LLMService] Model unloaded.");
        }

        // ── Streaming Chat ─────────────────────────────────

        /// <summary>
        /// Streams a chat completion token-by-token. Calls <paramref name="onToken"/>
        /// for each generated token and <paramref name="onDone"/> when finished.
        /// Falls back to null output if model is not loaded.
        /// </summary>
        /// <param name="systemPrompt">System prompt defining LLM behavior.</param>
        /// <param name="userMessage">The user's input message.</param>
        /// <param name="onToken">Callback invoked for each generated token.</param>
        /// <param name="onDone">Callback invoked when generation is complete.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task StreamChatAsync(
            string systemPrompt,
            string userMessage,
            Action<string> onToken,
            Action onDone,
            CancellationToken ct = default)
        {
            if (!_isLoaded)
            {
                Debug.LogWarning("[LLMService] Model not loaded, cannot stream.");
                onDone?.Invoke();
                return;
            }

            await _gate.WaitAsync(ct);
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                await Task.Run(async () =>
                {
                    var executor = new InteractiveExecutor(_context);
                    var session = new ChatSession(executor);
                    var history = new ChatHistory();
                    history.AddMessage(AuthorRole.System, systemPrompt);
                    history.AddMessage(AuthorRole.User, userMessage);

                    var inferenceParams = new InferenceParams
                    {
                        MaxTokens = MaxTokens,
                        Temperature = Temperature,
                        AntiPrompts = new[] { "User:", "\n\nUser:", "<|eot_id|>" }
                    };

                    await foreach (var token in session.ChatAsync(history, inferenceParams, cts.Token))
                    {
                        onToken?.Invoke(token);
                    }
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[LLMService] Streaming timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMService] Streaming error: {ex.Message}");
            }
            finally
            {
                _gate.Release();
                onDone?.Invoke();
            }
        }

        // ── Non-Streaming Chat ─────────────────────────────

        /// <summary>
        /// Sends a chat request and returns the full response string.
        /// Returns null if the model is not loaded or inference fails.
        /// </summary>
        /// <param name="systemPrompt">System prompt defining LLM behavior.</param>
        /// <param name="userMessage">The user's input message.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated response, or null on failure.</returns>
        public async Task<string> ChatAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken ct = default)
        {
            if (!_isLoaded)
            {
                Debug.LogWarning("[LLMService] Model not loaded, cannot chat.");
                return null;
            }

            var sb = new StringBuilder();
            var tcs = new TaskCompletionSource<bool>();

            await StreamChatAsync(
                systemPrompt,
                userMessage,
                token => sb.Append(token),
                () => tcs.TrySetResult(true),
                ct);

            await tcs.Task;
            var result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        // ── JSON-Mode Chat ─────────────────────────────────

        /// <summary>
        /// Sends a chat request with instructions to output JSON, then deserializes
        /// the response into <typeparamref name="T"/>. Returns default(T) on failure.
        /// 
        /// <para>
        /// Unlike Ollama's native JSON mode, this appends a JSON instruction to the
        /// system prompt and attempts to parse the result. The system prompt should
        /// already describe the expected JSON schema.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON response into.</typeparam>
        /// <param name="systemPrompt">System prompt (should describe JSON schema).</param>
        /// <param name="userMessage">The user's input message.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Deserialized response, or default(T) on failure.</returns>
        public async Task<T> ChatJsonAsync<T>(
            string systemPrompt,
            string userMessage,
            CancellationToken ct = default)
        {
            // Reinforce JSON output in the system prompt
            var jsonPrompt = systemPrompt +
                "\n\nIMPORTANT: Respond with valid JSON only. No markdown, no explanation.";

            var raw = await ChatAsync(jsonPrompt, userMessage, ct);
            if (raw == null) return default;

            // Try to extract JSON from the response (handle markdown code blocks)
            raw = ExtractJson(raw);

            try
            {
                return JsonConvert.DeserializeObject<T>(raw);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMService] JSON deserialization failed: {ex.Message}\nRaw: {raw}");
                return default;
            }
        }

        // ── Helpers ────────────────────────────────────────

        /// <summary>
        /// Resolves the full path to the model file.
        /// Uses <see cref="ModelPath"/> if set, otherwise defaults to
        /// Application.streamingAssetsPath/Models/.
        /// </summary>
        private string GetModelPath()
        {
            if (!string.IsNullOrEmpty(ModelPath))
                return Path.Combine(ModelPath, ModelFilename);

            return Path.Combine(Application.streamingAssetsPath, "Models", ModelFilename);
        }

        /// <summary>
        /// Extracts JSON content from a response that may be wrapped in
        /// markdown code blocks (```json ... ```).
        /// </summary>
        private static string ExtractJson(string raw)
        {
            if (raw == null) return null;
            raw = raw.Trim();

            // Strip markdown code block if present
            if (raw.StartsWith("```"))
            {
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline > 0)
                    raw = raw.Substring(firstNewline + 1);
                if (raw.EndsWith("```"))
                    raw = raw.Substring(0, raw.Length - 3);
                raw = raw.Trim();
            }

            // Find first { or [ and last } or ]
            int start = -1, end = -1;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '{' || raw[i] == '[') { start = i; break; }
            }
            for (int i = raw.Length - 1; i >= 0; i--)
            {
                if (raw[i] == '}' || raw[i] == ']') { end = i; break; }
            }

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return raw;
        }

        /// <summary>
        /// Releases all resources: model weights, context, and semaphore.
        /// </summary>
        public void Dispose()
        {
            UnloadModel();
            _gate?.Dispose();
        }
    }
}
