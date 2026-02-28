using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.LLM
{
    /// <summary>
    /// Provides static fallback clues from a pre-authored JSON file.
    /// Used when the LLM is unavailable, times out, or returns an invalid response.
    /// </summary>
    public class FallbackClueService
    {
        private Dictionary<string, FallbackWordEntry> _words;
        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;
        public int WordCount => _words?.Count ?? 0;

        /// <summary>
        /// Loads fallback clues from a raw JSON string.
        /// Accepts the format: { "words": { "elephant": { "category": "animals", "clues": [...] } } }
        /// </summary>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[FallbackClueService] Empty JSON provided.");
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<FallbackData>(json);
                if (data?.Words == null || data.Words.Count == 0)
                {
                    Debug.LogWarning("[FallbackClueService] No words found in fallback JSON.");
                    return;
                }

                // Normalize keys to lowercase for case-insensitive lookup
                _words = new Dictionary<string, FallbackWordEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in data.Words)
                {
                    _words[kvp.Key] = kvp.Value;
                }

                _isLoaded = true;
                Debug.Log($"[FallbackClueService] Loaded {_words.Count} fallback words.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackClueService] Failed to parse JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks whether a fallback clue exists for the given word.
        /// </summary>
        public bool HasWord(string word)
        {
            return _isLoaded
                && !string.IsNullOrWhiteSpace(word)
                && _words.ContainsKey(word.Trim());
        }

        /// <summary>
        /// Returns a fallback clue for the given word and clue number (1-based).
        /// Returns null if the word is not found.
        /// </summary>
        public ClueResponse GetClue(string word, int clueNumber)
        {
            if (!_isLoaded || string.IsNullOrWhiteSpace(word))
                return null;

            word = word.Trim();
            if (!_words.TryGetValue(word, out var entry))
                return null;

            if (entry.Clues == null || entry.Clues.Count == 0)
                return null;

            // Clamp clue number to available clues (1-based → 0-based index)
            int index = Mathf.Clamp(clueNumber - 1, 0, entry.Clues.Count - 1);

            return new ClueResponse
            {
                Clue = entry.Clues[index],
                Mood = "neutral",
                UsedFallbackModel = true,
                GenerationTimeMs = 0f
            };
        }

        // ── JSON Deserialization Models ─────────────────────

        [Serializable]
        private class FallbackData
        {
            [JsonProperty("words")]
            public Dictionary<string, FallbackWordEntry> Words { get; set; }
        }

        [Serializable]
        private class FallbackWordEntry
        {
            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("clues")]
            public List<string> Clues { get; set; }
        }
    }
}
