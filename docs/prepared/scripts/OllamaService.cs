using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Spellwright.LLM
{
    /// <summary>
    /// Async HTTP client for the Ollama REST API. Supports streaming (typewriter),
    /// non-streaming, and JSON-mode completions with a primary→fallback model chain.
    /// </summary>
    public class OllamaService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        /// <summary>Ollama base URL, e.g. "http://localhost:11434".</summary>
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>Primary model tag, e.g. "qwen2.5:7b".</summary>
        public string PrimaryModel { get; set; } = "qwen2.5:7b";

        /// <summary>Fallback model tag, e.g. "llama3.2:3b".</summary>
        public string FallbackModel { get; set; } = "llama3.2:3b";

        /// <summary>Timeout per request in seconds.</summary>
        public float TimeoutSeconds { get; set; } = 15f;

        /// <summary>LLM generation options (temperature, num_predict, etc.).</summary>
        public Dictionary<string, object> DefaultOptions { get; set; } = new Dictionary<string, object>
        {
            ["temperature"] = 0.8,
            ["num_predict"] = 200
        };

        public OllamaService()
        {
            _http = new HttpClient();
        }

        public OllamaService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ── Model Availability ─────────────────────────────

        /// <summary>
        /// Checks which of the configured models are locally available.
        /// Returns a tuple of (primaryAvailable, fallbackAvailable).
        /// </summary>
        public async Task<(bool Primary, bool Fallback)> CheckModelsAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}/api/tags", ct);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                var models = obj["models"] as JArray ?? new JArray();

                bool primaryFound = false, fallbackFound = false;
                foreach (var m in models)
                {
                    var name = m["name"]?.ToString() ?? "";
                    if (name == PrimaryModel || name.StartsWith(PrimaryModel + ":"))
                        primaryFound = true;
                    if (name == FallbackModel || name.StartsWith(FallbackModel + ":"))
                        fallbackFound = true;
                }

                return (primaryFound, fallbackFound);
            }
            catch
            {
                return (false, false);
            }
        }

        // ── Streaming Chat ─────────────────────────────────

        /// <summary>
        /// Streams a chat completion token-by-token. Calls <paramref name="onToken"/>
        /// for each token and <paramref name="onDone"/> when generation finishes.
        /// Uses the fallback chain: primary → fallback → null.
        /// </summary>
        public async Task StreamChatAsync(
            string systemPrompt,
            string userMessage,
            Action<string> onToken,
            Action onDone,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                bool success = await TryStreamModel(PrimaryModel, systemPrompt, userMessage, onToken, ct);
                if (!success)
                    success = await TryStreamModel(FallbackModel, systemPrompt, userMessage, onToken, ct);

                onDone?.Invoke();
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<bool> TryStreamModel(
            string model, string systemPrompt, string userMessage,
            Action<string> onToken, CancellationToken ct)
        {
            try
            {
                var payload = BuildPayload(model, systemPrompt, userMessage, stream: true, jsonMode: false);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/chat") { Content = content };

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    var obj = JObject.Parse(line);
                    var token = obj["message"]?["content"]?.ToString();
                    if (!string.IsNullOrEmpty(token))
                        onToken?.Invoke(token);

                    if (obj["done"]?.Value<bool>() == true)
                        break;
                }

                return true;
            }
            catch (Exception)
            {
                // TODO: Log via Debug.LogWarning in Unity.
                return false;
            }
        }

        // ── Non-Streaming Chat ─────────────────────────────

        /// <summary>
        /// Sends a non-streaming chat request and returns the full response string.
        /// Returns null if both primary and fallback models fail.
        /// </summary>
        public async Task<string> ChatAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var result = await TryChatModel(PrimaryModel, systemPrompt, userMessage, jsonMode: false, ct);
                if (result != null) return result;

                return await TryChatModel(FallbackModel, systemPrompt, userMessage, jsonMode: false, ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        // ── JSON-Mode Chat ─────────────────────────────────

        /// <summary>
        /// Sends a non-streaming chat request with Ollama's JSON format mode
        /// and deserializes the response into <typeparamref name="T"/>.
        /// Returns default(T) if both models fail or deserialization fails.
        /// </summary>
        public async Task<T> ChatJsonAsync<T>(
            string systemPrompt,
            string userMessage,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var raw = await TryChatModel(PrimaryModel, systemPrompt, userMessage, jsonMode: true, ct);
                if (raw == null)
                    raw = await TryChatModel(FallbackModel, systemPrompt, userMessage, jsonMode: true, ct);

                if (raw == null) return default;

                try
                {
                    return JsonConvert.DeserializeObject<T>(raw);
                }
                catch
                {
                    // TODO: Log deserialization failure via Debug.LogWarning.
                    return default;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        // ── Internal Helpers ───────────────────────────────

        private async Task<string> TryChatModel(
            string model, string systemPrompt, string userMessage,
            bool jsonMode, CancellationToken ct)
        {
            try
            {
                var payload = BuildPayload(model, systemPrompt, userMessage, stream: false, jsonMode: jsonMode);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                var response = await _http.PostAsync($"{BaseUrl}/api/chat", content, cts.Token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                return obj["message"]?["content"]?.ToString();
            }
            catch (Exception)
            {
                // TODO: Log via Debug.LogWarning in Unity.
                return null;
            }
        }

        private string BuildPayload(string model, string systemPrompt, string userMessage, bool stream, bool jsonMode)
        {
            var messages = new List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
                messages.Add(new { role = "system", content = systemPrompt });
            messages.Add(new { role = "user", content = userMessage });

            var obj = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = messages,
                ["stream"] = stream,
                ["options"] = DefaultOptions
            };

            if (jsonMode)
            {
                obj["format"] = "json";
            }

            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Sends a keep_alive request to control model loading/unloading in VRAM.
        /// Use "5m" to keep loaded, "0" to unload immediately.
        /// </summary>
        public async Task SetKeepAliveAsync(string model, string keepAlive, CancellationToken ct = default)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    model,
                    keep_alive = keepAlive
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                await _http.PostAsync($"{BaseUrl}/api/generate", content, ct);
            }
            catch
            {
                // Best-effort; ignore failures.
            }
        }

        public void Dispose()
        {
            _gate?.Dispose();
            _http?.Dispose();
        }
    }
}
