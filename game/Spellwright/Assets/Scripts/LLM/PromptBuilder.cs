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
            List<string> activeTomeEffects,
            GameLanguage language = GameLanguage.English,
            DifficultyShift difficultyShift = DifficultyShift.Normal)
        {
            var system = BuildSystemPrompt(npc, category, clueNumber, activeTomeEffects, language);
            var user = BuildUserMessage(targetWord, category, clueNumber, previousGuesses, language, difficultyShift);
            return (system, user);
        }

        /// <summary>
        /// Builds a short prompt for the NPC's dramatic ultimatum line (final guess moment).
        /// </summary>
        public static (string SystemPrompt, string UserMessage) BuildUltimatumPrompt(
            NPCPromptData npc,
            string mood,
            string targetWord,
            GameLanguage language = GameLanguage.English)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"You are {npc.DisplayName}, a {npc.Archetype} in a word-guessing game.");
            sb.AppendLine($"Your current mood is: {mood}.");
            sb.AppendLine("The player has ONE guess left. This is the dramatic final moment.");
            sb.AppendLine("Deliver a single dramatic sentence — your final words before they guess.");
            sb.AppendLine("Do NOT reveal the answer. Do NOT mention the word.");
            sb.AppendLine("Keep it under 15 words. Be theatrical and in-character.");
            if (language == GameLanguage.Romanian)
                sb.AppendLine("Scrie in romana, natural si dramatic.");
            sb.AppendLine("Respond with ONLY the sentence. No JSON. No quotes.");

            string user = language == GameLanguage.Romanian
                ? $"Cuvantul secret este \"{targetWord}\". Spune ultima ta replica dramatica."
                : $"The secret word is \"{targetWord}\". Deliver your dramatic final line.";

            return (sb.ToString(), user);
        }

        // ── System Prompt ──────────────────────────────────

        private static string BuildSystemPrompt(
            NPCPromptData npc,
            string category,
            int clueNumber,
            List<string> activeTomeEffects,
            GameLanguage language)
        {
            var sb = new StringBuilder();

            // Language instruction — at the very top for maximum compliance
            if (language == GameLanguage.Romanian)
            {
                sb.AppendLine("LIMBA: Scrie TOATE indiciile in limba romana, natural si fluent.");
                sb.AppendLine("Nu traduce din engleza — gandeste si scrie direct in romana.");
                sb.AppendLine("Foloseste un stil conversational, ca si cum ai vorbi cu un prieten roman.");
                sb.AppendLine("Evita constructii artificiale sau formale. Scrie simplu si clar.");
                sb.AppendLine();
            }

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

            // Language reminder (reinforced)
            if (language == GameLanguage.Romanian)
            {
                sb.AppendLine("- IMPORTANT: Scrie indiciul DOAR in romana, nu in engleza.");
                sb.AppendLine("- Foloseste expresii naturale romanesti, nu traduceri mot-a-mot din engleza.");
            }

            // Boss constraint
            if (npc.IsBoss && !string.IsNullOrEmpty(npc.BossConstraint))
            {
                sb.AppendLine($"- BOSS CONSTRAINT: {npc.BossConstraint}");
            }

            // Rival personality escalation
            var rivalSystem = Run.RivalSystem.Instance;
            if (rivalSystem != null && rivalSystem.HasRival && rivalSystem.IsRival(npc.Archetype))
            {
                sb.AppendLine();
                int tier = rivalSystem.RivalTier;
                if (tier <= 1)
                    sb.AppendLine("RIVAL CONTEXT: This is a newcomer. Be dismissive and unimpressed. You doubt they'll last.");
                else if (tier == 2)
                    sb.AppendLine("RIVAL CONTEXT: This player has beaten you before. You're irritated. Be more aggressive and competitive.");
                else
                    sb.AppendLine("RIVAL CONTEXT: This player keeps coming back and winning. You're desperate. Your pride is wounded. Be intense, dramatic, and pull no punches.");
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
            sb.AppendLine("Respond ONLY with a single JSON object, no other text:");
            if (language == GameLanguage.Romanian)
            {
                sb.AppendLine("{\"clue\": \"indiciul tau in romana\", \"mood\": \"neutral|amused|cryptic|frustrated|excited\"}");
                sb.AppendLine();
                sb.AppendLine("Exemplu (pentru cuvantul \"elefant\"):");
                sb.AppendLine("{\"clue\": \"Un urias bland cu trompa lunga, cel mai mare animal de pe uscat.\", \"mood\": \"neutral\"}");
            }
            else
            {
                sb.AppendLine("{\"clue\": \"your clue text\", \"mood\": \"neutral|amused|cryptic|frustrated|excited\"}");
            }

            return sb.ToString();
        }

        // ── User Message ───────────────────────────────────

        private static string BuildUserMessage(
            string targetWord,
            string category,
            int clueNumber,
            List<string> previousGuesses,
            GameLanguage language = GameLanguage.English,
            DifficultyShift difficultyShift = DifficultyShift.Normal)
        {
            var sb = new StringBuilder();

            bool isPhrase = targetWord != null && targetWord.Contains(' ');
            if (isPhrase)
            {
                int wordCount = targetWord.Split(' ').Length;
                int letterCount = targetWord.Replace(" ", "").Length;
                sb.AppendLine($"The secret phrase is \"{targetWord}\" (category: {category}, {wordCount} words, {letterCount} letters).");
                sb.AppendLine("NEVER reveal which individual words make up the phrase.");
            }
            else
            {
                sb.AppendLine($"The secret word is \"{targetWord}\" (category: {category}, {targetWord.Length} letters).");
            }

            if (previousGuesses != null && previousGuesses.Count > 0)
            {
                sb.Append("The player has guessed: ");
                sb.AppendLine(string.Join(", ", previousGuesses.ConvertAll(g => $"\"{g}\" (wrong)")));
            }

            sb.AppendLine($"Give clue #{clueNumber}. Remember: more specific than previous clues.");

            // Adaptive difficulty hints based on NPC mood shift
            if (difficultyShift == DifficultyShift.Mercy)
                sb.AppendLine("The player is struggling. Give a clearer, more direct hint.");
            else if (difficultyShift == DifficultyShift.Cruel)
                sb.AppendLine("The player is doing well. Be more oblique and challenging with your hint.");

            if (language == GameLanguage.Romanian)
                sb.AppendLine("Scrie indiciul in romana. Raspunde DOAR cu JSON.");
            else
                sb.AppendLine("Respond ONLY with JSON.");

            return sb.ToString();
        }
    }
}
