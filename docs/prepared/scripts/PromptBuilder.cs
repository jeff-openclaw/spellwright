using System.Collections.Generic;
using System.Text;
using Spellwright.Data;

namespace Spellwright.LLM
{
    /// <summary>
    /// Assembles system and user prompts for LLM clue generation,
    /// handling NPC personality, boss constraints, and Tome modifiers.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// Builds a (systemPrompt, userMessage) tuple for clue generation.
        /// </summary>
        /// <param name="npc">NPC personality and prompt template data.</param>
        /// <param name="targetWord">The secret word the player must guess.</param>
        /// <param name="category">Word category shown to the player (e.g. "Animals").</param>
        /// <param name="clueNumber">1-based clue index (each successive clue should be more specific).</param>
        /// <param name="previousGuesses">Words the player has already guessed (may be empty).</param>
        /// <param name="activeTomeEffects">Display names of active Tome effects that modify prompts.</param>
        /// <returns>A tuple of (systemPrompt, userMessage).</returns>
        public static (string SystemPrompt, string UserMessage) BuildCluePrompt(
            NPCPromptData npc,
            string targetWord,
            string category,
            int clueNumber,
            List<string> previousGuesses,
            List<string> activeTomeEffects)
        {
            var system = BuildSystemPrompt(npc, category, clueNumber, activeTomeEffects);
            var user = BuildUserMessage(targetWord, category, clueNumber, previousGuesses);
            return (system, user);
        }

        // ── System Prompt ──────────────────────────────────

        private static string BuildSystemPrompt(
            NPCPromptData npc,
            string category,
            int clueNumber,
            List<string> activeTomeEffects)
        {
            var sb = new StringBuilder();

            // NPC identity
            sb.AppendLine($"You are {npc.DisplayName}, a {npc.Archetype} in a magical word-guessing game.");
            sb.AppendLine();

            // NPC-specific personality prompt
            if (!string.IsNullOrEmpty(npc.SystemPromptTemplate))
            {
                var personalized = npc.SystemPromptTemplate
                    .Replace("{displayName}", npc.DisplayName)
                    .Replace("{archetype}", npc.Archetype.ToString())
                    .Replace("{category}", category)
                    .Replace("{clueNumber}", clueNumber.ToString());
                sb.AppendLine(personalized);
                sb.AppendLine();
            }

            // Core rules
            sb.AppendLine("RULES:");
            sb.AppendLine("- The player is trying to guess a secret word. You give clues.");
            sb.AppendLine("- NEVER say the word directly. NEVER use the word in your clue.");
            sb.AppendLine("- NEVER use a word that contains the secret word as a substring.");
            sb.AppendLine("- Your clue should be 1-2 sentences maximum.");
            sb.AppendLine($"- The word category is \"{category}\".");
            sb.AppendLine($"- This is clue #{clueNumber}. Each clue should be MORE specific than the last.");

            // Boss constraint
            if (npc.IsBoss && !string.IsNullOrEmpty(npc.BossConstraint))
            {
                sb.AppendLine($"- BOSS CONSTRAINT: {npc.BossConstraint}");
            }

            // Tome modifiers
            if (activeTomeEffects != null && activeTomeEffects.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ACTIVE MODIFIERS:");
                foreach (var effect in activeTomeEffects)
                {
                    sb.AppendLine($"- {effect}");
                }
            }

            // Response format
            sb.AppendLine();
            sb.AppendLine("Respond in JSON format:");
            sb.AppendLine("{\"clue\": \"your clue text\", \"mood\": \"neutral|amused|cryptic|frustrated|excited\"}");

            return sb.ToString();
        }

        // ── User Message ───────────────────────────────────

        private static string BuildUserMessage(
            string targetWord,
            string category,
            int clueNumber,
            List<string> previousGuesses)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"The secret word is \"{targetWord}\" (category: {category}, {targetWord.Length} letters).");

            if (previousGuesses != null && previousGuesses.Count > 0)
            {
                sb.Append("The player has guessed: ");
                sb.AppendLine(string.Join(", ", previousGuesses.ConvertAll(g => $"\"{g}\" (wrong)")));
            }

            sb.AppendLine($"Give clue #{clueNumber}. Remember: more specific than previous clues.");

            return sb.ToString();
        }
    }
}
