using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Spellwright.Data;

namespace Spellwright.LLM
{
    /// <summary>
    /// Parses raw LLM output into structured <see cref="ClueResponse"/> objects.
    /// Uses JSON as the primary strategy with regex fallback.
    /// </summary>
    public static class ResponseParser
    {
        /// <summary>
        /// Attempts to parse a clue response from raw LLM output.
        /// Returns null if the clue contains the target word (safety rejection).
        /// </summary>
        /// <param name="raw">Raw text from the LLM.</param>
        /// <param name="targetWord">The secret word; used for leakage detection.</param>
        /// <returns>A <see cref="ClueResponse"/>, or null if parsing fails or the clue is unsafe.</returns>
        public static ClueResponse ParseClueResponse(string raw, string targetWord = null)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            ClueResponse result = TryParseJson(raw) ?? TryParseRegex(raw);

            if (result == null)
                return null;

            // Safety: reject if clue contains the target word
            if (!string.IsNullOrEmpty(targetWord) && ContainsTargetWord(result.Clue, targetWord))
                return null;

            return result;
        }

        // ── JSON Parse ─────────────────────────────────────

        private static ClueResponse TryParseJson(string raw)
        {
            try
            {
                // The LLM may wrap JSON in markdown code fences; strip them.
                var cleaned = raw.Trim();
                if (cleaned.StartsWith("```"))
                {
                    var firstNewline = cleaned.IndexOf('\n');
                    if (firstNewline > 0) cleaned = cleaned.Substring(firstNewline + 1);
                    if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);
                    cleaned = cleaned.Trim();
                }

                var parsed = JsonConvert.DeserializeAnonymousType(cleaned, new { clue = "", mood = "" });
                if (!string.IsNullOrWhiteSpace(parsed?.clue))
                {
                    return new ClueResponse
                    {
                        Clue = parsed.clue.Trim(),
                        Mood = string.IsNullOrWhiteSpace(parsed.mood) ? "neutral" : parsed.mood.Trim()
                    };
                }
            }
            catch (JsonException)
            {
                // Fall through to regex.
            }

            return null;
        }

        // ── Regex Fallback ─────────────────────────────────

        private static readonly Regex ClueFieldRegex =
            new Regex(@"""clue""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MoodFieldRegex =
            new Regex(@"""mood""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static ClueResponse TryParseRegex(string raw)
        {
            // Try to extract clue field via regex (handles malformed JSON).
            var clueMatch = ClueFieldRegex.Match(raw);
            if (clueMatch.Success)
            {
                var moodMatch = MoodFieldRegex.Match(raw);
                return new ClueResponse
                {
                    Clue = clueMatch.Groups[1].Value.Trim(),
                    Mood = moodMatch.Success ? moodMatch.Groups[1].Value.Trim() : "neutral"
                };
            }

            // Last resort: treat the first 1-2 sentences as the clue.
            var sentences = raw.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            if (sentences.Length > 0)
            {
                var clue = sentences.Length >= 2
                    ? $"{sentences[0].Trim()}. {sentences[1].Trim()}."
                    : $"{sentences[0].Trim()}.";

                return new ClueResponse { Clue = clue, Mood = "neutral" };
            }

            return null;
        }

        // ── Safety ─────────────────────────────────────────

        /// <summary>
        /// Checks whether the clue contains the target word as a standalone word.
        /// </summary>
        private static bool ContainsTargetWord(string clue, string targetWord)
        {
            if (string.IsNullOrEmpty(clue) || string.IsNullOrEmpty(targetWord))
                return false;

            var pattern = $@"\b{Regex.Escape(targetWord)}\b";
            return Regex.IsMatch(clue, pattern, RegexOptions.IgnoreCase);
        }
    }
}
